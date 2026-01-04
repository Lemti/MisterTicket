namespace MisterTicket.Api.DTOs
{
    public class CreateReservationDto
    {
        public int EventId { get; set; }
        public List<int> SeatIds { get; set; } = new List<int>(); // Liste des IDs de sièges à réserver
    }
}