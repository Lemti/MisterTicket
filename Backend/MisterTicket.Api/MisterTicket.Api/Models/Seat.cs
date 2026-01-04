namespace MisterTicket.Api.Models
{
    public class Seat
    {
        public int Id { get; set; }
        public string SeatNumber { get; set; } = string.Empty; // Ex: "A-12"
        public string Row { get; set; } = string.Empty; // Ex: "A", "B", "C"
        public string Zone { get; set; } = string.Empty; // Ex: "VIP", "Tribune Centrale", "Gradin"
        public decimal Price { get; set; } // Tarif selon la zone
        public string Status { get; set; } = "Available"; // Available, Reserved, Paid

        // Relation avec le court
        public int CourtId { get; set; }
        public Court Court { get; set; } = null!;

        // Relations
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }
}