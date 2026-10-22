using Microsoft.AspNetCore.SignalR;

namespace TaskApi;

public sealed class TaskHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
