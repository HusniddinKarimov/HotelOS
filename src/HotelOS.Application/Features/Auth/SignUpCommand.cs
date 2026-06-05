using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Auth;

/// <summary>
/// Public self-registration. Creates a basic <c>User</c> account and returns
/// tokens so the new user is signed in immediately.
/// </summary>
public record SignUpCommand(string Username, string Email, string Password, string FullName) : IRequest<AuthResponse>;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Matches("^[a-zA-Z0-9._-]{3,50}$")
            .WithMessage("Username must be 3–50 chars: letters, digits, . _ -");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
    }
}

public class SignUpCommandHandler : IRequestHandler<SignUpCommand, AuthResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IAuditLogger _audit;

    public SignUpCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt, IAuditLogger audit)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<AuthResponse> Handle(SignUpCommand request, CancellationToken ct)
    {
        var users = _uow.Repository<User>();
        var roles = _uow.Repository<Role>();

        if (await users.AnyAsync(u => u.Username == request.Username, ct))
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        if (await users.AnyAsync(u => u.Email == request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var role = await roles.FirstOrDefaultAsync(r => r.Name == RoleNames.User, ct)
            ?? throw new NotFoundException("Role", RoleNames.User);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = _hasher.Hash(request.Password),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
        };
        await users.AddAsync(user, ct);

        // Issue tokens straight away (auto sign-in).
        var tokens = _jwt.CreateTokens(user);
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt;
        user.LastLoginAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("SignUp", nameof(User), user.Id.ToString(), $"Self-registered '{user.Username}'.", ct);

        return new AuthResponse(
            tokens.AccessToken, tokens.AccessTokenExpiresAt,
            tokens.RefreshToken, tokens.RefreshTokenExpiresAt,
            new UserDto(user.Id, user.Username, user.Email, user.FullName, role.Name, user.IsActive));
    }
}
