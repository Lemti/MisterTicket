namespace MisterTicket.Api.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string PaymentReference { get; set; } = string.Empty; // Référence unique du paiement
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // CreditCard, PayPal, BankTransfer (fictif)
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        // Relation
        public int ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;
    }
}