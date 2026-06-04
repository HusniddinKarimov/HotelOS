using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using MediatR;

namespace HotelOS.Application.Features.Guests;

/// <summary>Registers a new guest record.</summary>
public record RegisterGuestCommand(
    string FullName,
    string Email,
    string Phone,
    string? Nationality,
    string? PassportNumber) : IRequest<GuestDto>;

public class RegisterGuestCommandValidator : AbstractValidator<RegisterGuestCommand>
{
    public RegisterGuestCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40)
            .Matches(@"^[0-9+\-\s()]{6,40}$").WithMessage("Phone number is invalid.");
        RuleFor(x => x.Nationality).MaximumLength(60);
        RuleFor(x => x.PassportNumber).MaximumLength(40);
    }
}

public class RegisterGuestCommandHandler : IRequestHandler<RegisterGuestCommand, GuestDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _audit;

    public RegisterGuestCommandHandler(IUnitOfWork uow, IAuditLogger audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<GuestDto> Handle(RegisterGuestCommand request, CancellationToken ct)
    {
        var guest = new Guest
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Nationality = request.Nationality?.Trim(),
            PassportNumber = request.PassportNumber?.Trim()
        };

        await _uow.Repository<Guest>().AddAsync(guest, ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("RegisterGuest", nameof(Guest), guest.Id.ToString(), guest.FullName, ct);

        return guest.ToDto();
    }
}
