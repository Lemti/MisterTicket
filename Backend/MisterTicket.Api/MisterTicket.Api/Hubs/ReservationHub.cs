using Microsoft.AspNetCore.SignalR;

namespace MisterTicket.Api.Hubs
{
    public class ReservationHub : Hub
    {
        // Méthode appelée quand un client se connecte
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
        }

        // Méthode appelée quand un client se déconnecte
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        }

        // Méthode pour rejoindre un groupe (par exemple par eventId)
        public async Task JoinEventGroup(int eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Event_{eventId}");
            await Clients.Caller.SendAsync("JoinedEventGroup", eventId);
        }

        // Méthode pour quitter un groupe
        public async Task LeaveEventGroup(int eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Event_{eventId}");
            await Clients.Caller.SendAsync("LeftEventGroup", eventId);
        }
    }
}