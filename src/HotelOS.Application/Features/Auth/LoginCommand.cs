using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Auth;

/// <summary>Authenticates a user and issues access + refresh tokens.</summary>
public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditLogger _audit;

    public LoginCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt, IAuditLogger audit)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var users = _uow.Repository<User>();
        var user = await users.Query(tracking: true)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username, ct);

        if (user is null || !user.IsActive || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationException("Invalid username or password.");

        var tokens = _jwt.CreateTokens(user);
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        user.LastLoginAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync("Login", nameof(User), user.Id.ToString(), $"User '{user.Username}' signed in.", ct);

        return new AuthResponse(
            tokens.AccessToken, tokens.AccessTokenExpiresAt,
            tokens.RefreshToken, tokens.RefreshTokenExpiresAt,
            new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role.Name, user.IsActive));
    }
}
