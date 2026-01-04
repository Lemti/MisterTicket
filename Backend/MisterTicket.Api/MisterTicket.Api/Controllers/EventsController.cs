using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Data;
using MisterTicket.Api.DTOs;
using MisterTicket.Api.Models;
using System.Security.Claims;

namespace MisterTicket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Court)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Category = e.Category,
                    Round = e.Round,
                    CourtId = e.CourtId,
                    CourtName = e.Court.Name
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/my (Organisateur uniquement)
        [HttpGet("my")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var events = await _context.Events
                .Include(e => e.Court)
                .Where(e => e.OrganizerId == userId)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    EventDate = e.EventDate,
                    Category = e.Category,
                    Round = e.Round,
                    CourtId = e.CourtId,
                    CourtName = e.Court.Name
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Court)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                return NotFound();
            }

            var eventDto = new EventDto
            {
                Id = eventItem.Id,
                Name = eventItem.Name,
                Description = eventItem.Description,
                EventDate = eventItem.EventDate,
                Category = eventItem.Category,
                Round = eventItem.Round,
                CourtId = eventItem.CourtId,
                CourtName = eventItem.Court.Name
            };

            return Ok(eventDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto createEventDto)
        {
            try
            {
                Console.WriteLine("=== CREATE EVENT START ===");
                Console.WriteLine($"User: {User.Identity?.Name}");
                Console.WriteLine($"DTO: Name={createEventDto.Name}, CourtId={createEventDto.CourtId}");

                // SIMPLIFIER : Validation minimale
                if (string.IsNullOrWhiteSpace(createEventDto.Name))
                    return BadRequest("Nom d'événement requis");

                // Vérifier court
                var court = await _context.Courts.FindAsync(createEventDto.CourtId);
                if (court == null)
                {
                    Console.WriteLine($"Court {createEventDto.CourtId} non trouvé");
                    return BadRequest($"Court {createEventDto.CourtId} non trouvé");
                }

                Console.WriteLine($"Court trouvé: {court.Name}");

                // Créer sans OrganizerId pour tester
                var eventItem = new Event
                {
                    Name = createEventDto.Name,
                    Description = createEventDto.Description ?? "",
                    EventDate = createEventDto.EventDate,
                    Category = createEventDto.Category ?? "Simple Hommes",
                    Round = createEventDto.Round ?? "Finale",
                    CourtId = createEventDto.CourtId,
                    // OrganizerId = null // LAISSER NULL POUR TEST
                };

                Console.WriteLine("Event créé, sauvegarde...");

                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Event sauvegardé avec ID: {eventItem.Id}");

                var eventDto = new EventDto
                {
                    Id = eventItem.Id,
                    Name = eventItem.Name,
                    Description = eventItem.Description,
                    EventDate = eventItem.EventDate,
                    Category = eventItem.Category,
                    Round = eventItem.Round,
                    CourtId = eventItem.CourtId,
                    CourtName = court.Name,
                    OrganizerId = eventItem.OrganizerId // Peut être null
                };

                Console.WriteLine("=== CREATE EVENT SUCCESS ===");

                return CreatedAtAction(nameof(GetEvent), new { id = eventItem.Id }, eventDto);
            }
            catch (Exception ex)
            {
                // LOG DÉTAILLÉ
                Console.WriteLine($"=== CREATE EVENT ERROR ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }

                return StatusCode(500, new
                {
                    message = "Erreur interne serveur",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // DELETE: api/Events/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Reservations)
                    .ThenInclude(r => r.ReservationSeats)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
            {
                return NotFound(new { message = "Événement non trouvé" });
            }

            // OWNERSHIP: Organizer ne peut supprimer que SES événements
            if (User.IsInRole("Organizer") && !User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                if (eventItem.OrganizerId != userId)
                {
                    return Forbid();
                }
            }

            // Vérifier les réservations
            if (eventItem.Reservations != null && eventItem.Reservations.Any())
            {
                // Compter les réservations payées
                var paidReservations = eventItem.Reservations
                    .Where(r => r.Status == "Paid")
                    .Count();

                if (paidReservations > 0)
                {
                    return BadRequest(new
                    {
                        message = $"Impossible de supprimer : {paidReservations} réservation(s) payée(s) existent",
                        paidReservations = paidReservations,
                        totalReservations = eventItem.Reservations.Count
                    });
                }

                // Annuler les réservations en attente
                foreach (var reservation in eventItem.Reservations.Where(r => r.Status == "Pending"))
                {
                    reservation.Status = "Cancelled";
                    // Libérer les sièges
                    foreach (var rs in reservation.ReservationSeats)
                    {
                        var seat = await _context.Seats.FindAsync(rs.SeatId);
                        if (seat != null)
                        {
                            seat.Status = "Available";
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}