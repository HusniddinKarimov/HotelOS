using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace HotelOS.Api.Realtime;

/// <summary>
/// Implements the application's realtime port using a SignalR hub context, and
/// persists each notification so it also appears in the in-app notification list.
/// </summary>
public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<DashboardHub> _hub;
    private readonly IUnitOfWork _uow;

    public SignalRNotifier(IHubContext<DashboardHub> hub, IUnitOfWork uow)
    {
        _hub = hub;
        _uow = uow;
    }

    public Task BroadcastDashboardAsync(object snapshot, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("dashboard", snapshot, ct);

    public async Task NotifyAsync(NotificationType type, string message, string? targetRole = null, CancellationToken ct = default)
    {
        // Persist the in-app notification.
        await _uow.Repository<Notification>().AddAsync(new Notification
        {
            Type = type,
            Message = message,
            TargetRole = targetRole
        }, ct);
        await _uow.SaveChangesAsync(ct);

        // Push it live.
        var payload = new { type = type.ToString(), message, at = DateTime.UtcNow };
        if (string.IsNullOrEmpty(targetRole))
            await _hub.Clients.All.SendAsync("notification", payload, ct);
        else
            await _hub.Clients.Group(targetRole).SendAsync("notification", payload, ct);
    }

    public Task ActivityAsync(string message, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("activity", new { message, at = DateTime.UtcNow }, ct);
}
