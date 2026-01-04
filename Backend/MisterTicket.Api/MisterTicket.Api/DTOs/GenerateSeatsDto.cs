namespace MisterTicket.Api.DTOs
{
    public class GenerateSeatsDto
    {
        public string Template { get; set; } = string.Empty; // "small", "medium", "large"
        public decimal VipPrice { get; set; }
        public decimal TribunePrice { get; set; }
        public decimal GradinPrice { get; set; }
    }
}