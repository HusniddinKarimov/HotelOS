using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Auth;

/// <summary>Exchanges a valid refresh token for a new access + refresh token pair (rotation).</summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IUnitOfWork uow, IJwtTokenService jwt)
    {
        _uow = uow;
        _jwt = jwt;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var users = _uow.Repository<User>();
        var user = await users.Query(tracking: true)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new AuthenticationException("Invalid or expired refresh token.");

        var tokens = _jwt.CreateTokens(user);
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        await _uow.SaveChangesAsync(ct);

        return new AuthResponse(
            tokens.AccessToken, tokens.AccessTokenExpiresAt,
            tokens.RefreshToken, tokens.RefreshTokenExpiresAt,
            new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.IsActive));
    }
}
