namespace MisterTicket.Api.Models
{
    public class ReservationSeat
    {
        // Relations (clé composite)
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public int SeatId { get; set; }
        public Seat Seat { get; set; } = null!;
    }
}