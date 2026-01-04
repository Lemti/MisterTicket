namespace MisterTicket.Api.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled
        public decimal TotalAmount { get; set; }
        public DateTime? ExpiresAt { get; set; } // Réservation temporaire expire après X minutes

        // Relations
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();

        public Payment? Payment { get; set; }
    }
}