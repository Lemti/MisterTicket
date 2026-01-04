namespace MisterTicket.Api.DTOs
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Paid, Cancelled
        public decimal TotalAmount { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        public List<int> SeatIds { get; set; } = new List<int>();
        public List<string> SeatNumbers { get; set; } = new List<string>();
    }
}