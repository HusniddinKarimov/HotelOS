using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelOS.Api.Realtime;

/// <summary>
/// SignalR hub the dashboard and staff clients connect to for live updates.
/// Requires authentication; the server pushes "dashboard", "notification" and
/// "activity" messages to connected clients.
/// </summary>
[Authorize]
public class DashboardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Group connections by role so notifications can be targeted.
        var role = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(role))
            await Groups.AddToGroupAsync(Context.ConnectionId, role);
        await base.OnConnectedAsync();
    }
}
