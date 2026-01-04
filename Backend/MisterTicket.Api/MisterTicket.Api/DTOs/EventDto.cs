namespace MisterTicket.Api.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Round { get; set; } = string.Empty;
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public int? OrganizerId { get; set; } // Nullable

    }
}