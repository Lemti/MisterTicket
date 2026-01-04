namespace MisterTicket.Api.Models
{
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Ex: "Philippe-Chatrier", "Suzanne-Lenglen"
        public int Capacity { get; set; } // Nombre total de places
        public string Description { get; set; } = string.Empty;

        // Relations
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}