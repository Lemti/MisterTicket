namespace MisterTicket.Api.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Ex: "Finale Hommes - Nadal vs Djokovic"
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Category { get; set; } = string.Empty; // Simple Hommes, Simple Dames, Double, etc.
        public string Round { get; set; } = string.Empty; // Finale, Demi-finale, Quart, etc.

        // Relation avec le court
        public int CourtId { get; set; }
        public Court Court { get; set; } = null!;

        // NOUVEAU : Relation avec l'organisateur (nullable car Admin peut créer sans organisateur)
        public int? OrganizerId { get; set; }
        public User? Organizer { get; set; }

        // Relations
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}