namespace HotelOS.Application.Features.Auth;

/// <summary>Public view of a user (no password hash).</summary>
public record UserDto(Guid Id, string Username, string Email, string FullName, string Role, bool IsActive);

/// <summary>Returned on successful login or refresh.</summary>
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
