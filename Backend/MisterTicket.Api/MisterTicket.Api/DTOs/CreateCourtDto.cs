namespace MisterTicket.Api.DTOs
{
    public class CreateCourtDto
    {
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}