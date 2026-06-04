using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Infrastructure.Persistence;

namespace HotelOS.Infrastructure.Security;

/// <summary>Persists audit-log records, stamping them with the current user when known.</summary>
public class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string? entity = null, string? entityId = null, string? details = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            UserId = _currentUser.UserId,
            Username = _currentUser.Username,
            IpAddress = _currentUser.IpAddress
        });
        await _db.SaveChangesAsync(ct);
    }
}
