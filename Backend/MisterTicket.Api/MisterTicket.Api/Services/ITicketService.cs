namespace MisterTicket.Api.Services
{
    public interface ITicketService
    {
        byte[] GenerateTicketPdf(int reservationId, string userName, string eventName, List<string> seatNumbers, decimal totalAmount);
    }
}