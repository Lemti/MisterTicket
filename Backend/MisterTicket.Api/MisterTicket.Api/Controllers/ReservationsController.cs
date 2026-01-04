using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Data;
using MisterTicket.Api.DTOs;
using MisterTicket.Api.Hubs;
using MisterTicket.Api.Models;
using MisterTicket.Api.Services;
using System.Security.Claims;

namespace MisterTicket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ReservationHub> _hubContext;
        private readonly ITicketService _ticketService;

        public ReservationsController(ApplicationDbContext context, IHubContext<ReservationHub> hubContext, ITicketService ticketService)
        {
            _context = context;
            _hubContext = hubContext;
            _ticketService = ticketService;
        }

        // GET: api/Reservations (Admin/Organizer uniquement)
        [HttpGet]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    ReservationDate = r.ReservationDate,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    ExpiresAt = r.ExpiresAt,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    EventId = r.EventId,
                    EventName = r.Event.Name,
                    SeatIds = r.ReservationSeats.Select(rs => rs.SeatId).ToList(),
                    SeatNumbers = r.ReservationSeats.Select(rs => rs.Seat.SeatNumber).ToList()
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // GET: api/Reservations/my (Client uniquement - SES réservations)
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReservationDto>>> GetMyReservations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .Where(r => r.UserId == userId)
                .Select(r => new ReservationDto
                {
                    Id = r.Id,
                    ReservationDate = r.ReservationDate,
                    Status = r.Status,
                    TotalAmount = r.TotalAmount,
                    ExpiresAt = r.ExpiresAt,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    EventId = r.EventId,
                    EventName = r.Event.Name,
                    SeatIds = r.ReservationSeats.Select(rs => rs.SeatId).ToList(),
                    SeatNumbers = r.ReservationSeats.Select(rs => rs.Seat.SeatNumber).ToList()
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            var reservationDto = new ReservationDto
            {
                Id = reservation.Id,
                ReservationDate = reservation.ReservationDate,
                Status = reservation.Status,
                TotalAmount = reservation.TotalAmount,
                ExpiresAt = reservation.ExpiresAt,
                UserId = reservation.UserId,
                UserName = reservation.User.Name,
                EventId = reservation.EventId,
                EventName = reservation.Event.Name,
                SeatIds = reservation.ReservationSeats.Select(rs => rs.SeatId).ToList(),
                SeatNumbers = reservation.ReservationSeats.Select(rs => rs.Seat.SeatNumber).ToList()
            };

            return Ok(reservationDto);
        }

        // POST: api/Reservations
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createReservationDto)
        {
            try
            {
                // Récupérer l'utilisateur connecté depuis le JWT
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return BadRequest(new { message = "User not found" });
                }

                // Vérifier que l'événement existe
                var eventItem = await _context.Events.FindAsync(createReservationDto.EventId);
                if (eventItem == null)
                {
                    return BadRequest(new { message = "Event not found" });
                }

                // Vérifier que tous les sièges existent et sont disponibles
                var seats = await _context.Seats
                    .Where(s => createReservationDto.SeatIds.Contains(s.Id))
                    .ToListAsync();

                if (seats.Count != createReservationDto.SeatIds.Count)
                {
                    return BadRequest(new { message = "One or more seats not found" });
                }

                // Vérifier que tous les sièges sont disponibles
                var unavailableSeats = seats.Where(s => s.Status != "Available").ToList();
                if (unavailableSeats.Any())
                {
                    return BadRequest(new { message = $"Seats {string.Join(", ", unavailableSeats.Select(s => s.SeatNumber))} are not available" });
                }

                // Calculer le montant total
                var totalAmount = seats.Sum(s => s.Price);

                // Créer la réservation (temporaire pendant 15 minutes)
                var reservation = new Reservation
                {
                    UserId = userId,
                    EventId = createReservationDto.EventId,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Créer les liens entre réservation et sièges
                foreach (var seat in seats)
                {
                    var reservationSeat = new ReservationSeat
                    {
                        ReservationId = reservation.Id,
                        SeatId = seat.Id
                    };
                    _context.ReservationSeats.Add(reservationSeat);

                    // Marquer les sièges comme "Reserved" (temporaire)
                    seat.Status = "Reserved";
                }

                await _context.SaveChangesAsync();

                // Notifier tous les clients en temps réel
               /* await _hubContext.Clients.All.SendAsync("SeatReserved", new
                {
                    eventId = createReservationDto.EventId,
                    seatIds = seats.Select(s => s.Id).ToList(),
                    seatNumbers = seats.Select(s => s.SeatNumber).ToList()
                });
               */
                var reservationDto = new ReservationDto
                {
                    Id = reservation.Id,
                    ReservationDate = reservation.ReservationDate,
                    Status = reservation.Status,
                    TotalAmount = reservation.TotalAmount,
                    ExpiresAt = reservation.ExpiresAt,
                    UserId = reservation.UserId,
                    UserName = user.Name,
                    EventId = reservation.EventId,
                    EventName = eventItem.Name,
                    SeatIds = seats.Select(s => s.Id).ToList(),
                    SeatNumbers = seats.Select(s => s.SeatNumber).ToList()
                };

                return Ok(reservationDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERREUR CreateReservation: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"InnerException: {ex.InnerException?.Message}");

                return StatusCode(500, new
                {
                    message = "Erreur serveur",
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // PUT: api/Reservations/5/cancel
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.ReservationSeats)
                        .ThenInclude(rs => rs.Seat)
                    .Include(r => r.Payment)
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    return NotFound(new { message = "Réservation non trouvée" });
                }

                // DROITS : Admin peut TOUT annuler, sinon vérifier propriétaire
                // Dans ta méthode CancelReservation :
                if (!User.IsInRole("Admin"))
                {
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                    if (reservation.UserId != userId)
                    {
                        return StatusCode(403, new
                        {
                            message = "Vous ne pouvez annuler que vos propres réservations",
                            requiredUserId = reservation.UserId,
                            currentUserId = userId
                        });
                    }
                }

                // VÉRIFICATIONS :

                // 1. Déjà annulée ?
                if (reservation.Status == "Cancelled")
                {
                    return BadRequest(new { message = "Cette réservation est déjà annulée" });
                }

                // 2. Événement déjà passé ? (optionnel mais logique)
                if (reservation.Event.EventDate < DateTime.UtcNow)
                {
                    // On pourrait autoriser avec un message d'avertissement
                    // return BadRequest(new { message = "Impossible d'annuler une réservation pour un événement déjà passé" });
                }

                // 3. Politique : 24h avant (sauf Admin)
                var timeUntilEvent = reservation.Event.EventDate - DateTime.UtcNow;
                if (timeUntilEvent.TotalHours < 24 && !User.IsInRole("Admin"))
                {
                    // Admin peut bypass les 24h
                    return BadRequest(new
                    {
                        message = "Impossible d'annuler moins de 24h avant l'événement",
                        hoursRemaining = timeUntilEvent.TotalHours
                    });
                }

                // TOUT EST OK → PROCÉDER À L'ANNULATION

                // 1. Libérer les sièges
                foreach (var rs in reservation.ReservationSeats)
                {
                    rs.Seat.Status = "Available";
                }

                // 2. Mettre à jour le statut
                reservation.Status = "Cancelled";
                reservation.ExpiresAt = DateTime.UtcNow; // Marquer comme expiré

                // 3. Gérer le paiement si existe
                if (reservation.Payment != null)
                {
                    reservation.Payment.Status = "Refunded";
                    reservation.Payment.PaymentDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // 4. Notifier en temps réel
               /* await _hubContext.Clients.All.SendAsync("SeatReleased", new
                {
                    eventId = reservation.EventId,
                    seatIds = reservation.ReservationSeats.Select(rs => rs.SeatId).ToList(),
                    seatNumbers = reservation.ReservationSeats.Select(rs => rs.Seat.SeatNumber).ToList(),
                    cancelledBy = User.IsInRole("Admin") ? "admin" : "user"
                });*/

                return Ok(new
                {
                    message = "Réservation annulée avec succès",
                    reservationId = reservation.Id,
                    seatsFreed = reservation.ReservationSeats.Count,
                    refundAmount = reservation.TotalAmount,
                    status = "Cancelled"
                });
            }
            catch (Exception ex)
            {
                // Log l'erreur
                Console.WriteLine($"Erreur annulation réservation {id}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                return StatusCode(500, new
                {
                    message = "Erreur interne lors de l'annulation",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // GET: api/Reservations/5/ticket
        [HttpGet("{id}/ticket")]
        [Authorize]
        public async Task<IActionResult> DownloadTicket(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // OWNERSHIP: Client ne peut télécharger que SES billets (sauf Admin)
            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                if (reservation.UserId != userId)
                {
                    return Forbid();
                }
            }

            if (reservation.Status != "Paid")
            {
                return BadRequest(new { message = "Ticket can only be downloaded for paid reservations" });
            }

            var seatNumbers = reservation.ReservationSeats.Select(rs => rs.Seat.SeatNumber).ToList();

            var pdfBytes = _ticketService.GenerateTicketPdf(
                reservation.Id,
                reservation.User.Name,
                reservation.Event.Name,
                seatNumbers,
                reservation.TotalAmount
            );

            return File(pdfBytes, "application/pdf", $"Billet-Reservation-{reservation.Id}.pdf");
        }

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.ReservationSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}