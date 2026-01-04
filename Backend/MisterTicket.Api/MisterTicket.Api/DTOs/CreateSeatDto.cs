namespace MisterTicket.Api.DTOs
{
    public class CreateSeatDto
    {
        public string SeatNumber { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty; // VIP, Tribune, Gradin
        public decimal Price { get; set; }
        public int CourtId { get; set; }
    }
}