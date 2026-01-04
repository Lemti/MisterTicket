namespace MisterTicket.Api.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public int ReservationId { get; set; }
    }
}