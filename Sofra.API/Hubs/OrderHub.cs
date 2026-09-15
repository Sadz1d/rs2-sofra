using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Sofra.API.Constants;
using Sofra.API.Extensions;

namespace Sofra.API.Hubs;

/// <summary>
/// Grupe "user:{id}" (gost koji prati vlastitu narudzbu), "staff" (Konobar, Admin) i "kitchen" (Kuhar, Admin) -
/// clanstvo se odredjuje iskljucivo iz JWT claim-ova na konekciji, nikad iz parametra koji bi klijent poslao
/// (inace bi se gost mogao sam ubaciti u staff/kitchen grupu).
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{user.GetUserId()}");

        if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Konobar))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        }

        if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Kuhar))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
        }

        await base.OnConnectedAsync();
    }
}
