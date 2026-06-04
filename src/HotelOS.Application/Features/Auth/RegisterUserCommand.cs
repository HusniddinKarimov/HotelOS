using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Auth;

/// <summary>Creates a new staff user account (Administrator only).</summary>
public record RegisterUserCommand(string Username, string Email, string Password, string FullName, string RoleName)
    : IRequest<UserDto>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Matches("^[a-zA-Z0-9._-]{3,50}$")
            .WithMessage("Username must be 3–50 chars: letters, digits, . _ -");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;

    public RegisterUserCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IAuditLogger audit)
    {
        _uow = uow;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<UserDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var users = _uow.Repository<User>();
        var roles = _uow.Repository<Role>();

        if (await users.AnyAsync(u => u.Username == request.Username, ct))
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        if (await users.AnyAsync(u => u.Email == request.Email, ct))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        var role = await roles.FirstOrDefaultAsync(r => r.Name == request.RoleName, ct)
            ?? throw new NotFoundException("Role", request.RoleName);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            PasswordHash = _hasher.Hash(request.Password),
            RoleId = role.Id,
            IsActive = true
        };

        await users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("CreateUser", nameof(User), user.Id.ToString(), $"Created '{user.Username}' as {role.Name}.", ct);

        return new UserDto(user.Id, user.Username, user.Email, user.FullName, role.Name, user.IsActive);
    }
}
