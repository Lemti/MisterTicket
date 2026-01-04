using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Data;
using MisterTicket.Api.Hubs;

namespace MisterTicket.Api.Services
{
    public class ReservationExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<ReservationHub> _hubContext;
        private readonly ILogger<ReservationExpirationService> _logger;

        public ReservationExpirationService(
            IServiceProvider serviceProvider,
            IHubContext<ReservationHub> hubContext,
            ILogger<ReservationExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Reservation Expiration Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckExpiredReservations();
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Vérifie toutes les minutes
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Reservation Expiration Service");
                }
            }

            _logger.LogInformation("Reservation Expiration Service stopped");
        }

        private async Task CheckExpiredReservations()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Trouver toutes les réservations expirées qui sont encore "Pending"
            var expiredReservations = await context.Reservations
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .Where(r => r.Status == "Pending"
                         && r.ExpiresAt.HasValue
                         && r.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            if (expiredReservations.Any())
            {
                _logger.LogInformation($"Found {expiredReservations.Count} expired reservations");

                foreach (var reservation in expiredReservations)
                {
                    // Libérer les sièges
                    foreach (var rs in reservation.ReservationSeats)
                    {
                        rs.Seat.Status = "Available";
                    }

                    // Marquer la réservation comme annulée
                    reservation.Status = "Cancelled";

                    // Notifier en temps réel
                    await _hubContext.Clients.All.SendAsync("SeatReleased", new
                    {
                        eventId = reservation.EventId,
                        seatIds = reservation.ReservationSeats.Select(rs => rs.SeatId).ToList(),
                        reason = "Reservation expired"
                    });

                    _logger.LogInformation($"Reservation {reservation.Id} expired and cancelled");
                }

                await context.SaveChangesAsync();
            }
        }
    }
}