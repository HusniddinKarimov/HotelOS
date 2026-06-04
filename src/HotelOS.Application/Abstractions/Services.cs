using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;

namespace HotelOS.Application.Abstractions;

/// <summary>Result of issuing a JWT access token plus its refresh token.</summary>
public record TokenResult(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

/// <summary>Creates signed JWT access tokens and opaque refresh tokens.</summary>
public interface IJwtTokenService
{
    TokenResult CreateTokens(User user);
}

/// <summary>One-way password hashing and verification.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Ambient information about the authenticated caller.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

/// <summary>Pushes live updates to connected dashboards (implemented over SignalR).</summary>
public interface IRealtimeNotifier
{
    Task BroadcastDashboardAsync(object snapshot, CancellationToken ct = default);
    Task NotifyAsync(NotificationType type, string message, string? targetRole = null, CancellationToken ct = default);
    Task ActivityAsync(string message, CancellationToken ct = default);
}

/// <summary>Writes audit-log records for security-relevant actions.</summary>
public interface IAuditLogger
{
    Task LogAsync(string action, string? entity = null, string? entityId = null, string? details = null, CancellationToken ct = default);
}
