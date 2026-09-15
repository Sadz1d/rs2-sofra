using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sofra.API.Extensions;

namespace Sofra.API.Hubs;

/// <summary>
/// Grupa "user:{id}" po prijavljenom korisniku - clanstvo se odredjuje iskljucivo iz JWT claim-ova
/// na konekciji (Context.User), nikad iz parametra koji bi klijent mogao poslati.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }
}
