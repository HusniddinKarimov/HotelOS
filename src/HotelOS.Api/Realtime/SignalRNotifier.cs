using HotelOS.Application.Abstractions;
using HotelOS.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace HotelOS.Api.Realtime;

/// <summary>Implements the application's realtime port using a SignalR hub context.</summary>
public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<DashboardHub> _hub;

    public SignalRNotifier(IHubContext<DashboardHub> hub) => _hub = hub;

    public Task BroadcastDashboardAsync(object snapshot, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("dashboard", snapshot, ct);

    public Task NotifyAsync(NotificationType type, string message, string? targetRole = null, CancellationToken ct = default)
    {
        var payload = new { type = type.ToString(), message, at = DateTime.UtcNow };
        return string.IsNullOrEmpty(targetRole)
            ? _hub.Clients.All.SendAsync("notification", payload, ct)
            : _hub.Clients.Group(targetRole).SendAsync("notification", payload, ct);
    }

    public Task ActivityAsync(string message, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("activity", new { message, at = DateTime.UtcNow }, ct);
}
