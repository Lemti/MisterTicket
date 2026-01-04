namespace MisterTicket.Api.DTOs
{
    public class CreateEventDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Category { get; set; } = string.Empty; // Simple Hommes, Simple Dames, Double, etc.
        public string Round { get; set; } = string.Empty; // Finale, Demi-finale, Quart, etc.
        public int CourtId { get; set; }
    }
}