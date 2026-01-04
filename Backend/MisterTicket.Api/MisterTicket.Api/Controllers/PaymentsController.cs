using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Data;
using MisterTicket.Api.DTOs;
using MisterTicket.Api.Models;
using Microsoft.AspNetCore.SignalR;
using MisterTicket.Api.Hubs;

namespace MisterTicket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ReservationHub> _hubContext;

        public PaymentsController(ApplicationDbContext context, IHubContext<ReservationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetPayments()
        {
            var payments = await _context.Payments
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    PaymentReference = p.PaymentReference,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    ReservationId = p.ReservationId
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            var paymentDto = new PaymentDto
            {
                Id = payment.Id,
                PaymentReference = payment.PaymentReference,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                ReservationId = payment.ReservationId
            };

            return Ok(paymentDto);
        }

        // POST: api/Payments
        [HttpPost]
        public async Task<ActionResult<PaymentDto>> ProcessPayment(CreatePaymentDto createPaymentDto)
        {
            // Récupérer la réservation avec ses sièges
            var reservation = await _context.Reservations
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .FirstOrDefaultAsync(r => r.Id == createPaymentDto.ReservationId);

            if (reservation == null)
            {
                return BadRequest(new { message = "Reservation not found" });
            }

            // Vérifier que la réservation est en attente
            if (reservation.Status != "Pending")
            {
                return BadRequest(new { message = $"Reservation is already {reservation.Status}" });
            }

            // Vérifier que la réservation n'a pas expiré
            if (reservation.ExpiresAt.HasValue && reservation.ExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Reservation has expired" });
            }

            // Générer une référence de paiement unique
            var paymentReference = $"PAY-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            // Créer le paiement (fictif - toujours "Completed")
            var payment = new Payment
            {
                PaymentReference = paymentReference,
                Amount = reservation.TotalAmount,
                PaymentMethod = createPaymentDto.PaymentMethod,
                Status = "Completed", // Paiement fictif toujours réussi
                ReservationId = reservation.Id
            };

            _context.Payments.Add(payment);

            // Mettre à jour la réservation
            reservation.Status = "Paid";
            reservation.ExpiresAt = null; // Plus d'expiration une fois payé

            // Mettre à jour les sièges : Reserved → Paid
            foreach (var rs in reservation.ReservationSeats)
            {
                rs.Seat.Status = "Paid";
            }

            await _context.SaveChangesAsync();

            // Notifier que le paiement est effectué
            await _hubContext.Clients.All.SendAsync("PaymentCompleted", new
            {
                reservationId = reservation.Id,
                eventId = reservation.EventId,
                seatIds = reservation.ReservationSeats.Select(rs => rs.SeatId).ToList()
            });

            var paymentDto = new PaymentDto
            {
                Id = payment.Id,
                PaymentReference = payment.PaymentReference,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                ReservationId = payment.ReservationId
            };

            return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, paymentDto);
        }
    }
}