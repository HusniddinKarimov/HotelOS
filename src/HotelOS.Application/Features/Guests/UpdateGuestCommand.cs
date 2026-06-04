using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;

namespace HotelOS.Application.Features.Guests;

/// <summary>Updates an existing guest's details.</summary>
public record UpdateGuestCommand(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    string? Nationality,
    string? PassportNumber) : IRequest<GuestDto>;

public class UpdateGuestCommandValidator : AbstractValidator<UpdateGuestCommand>
{
    public UpdateGuestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40)
            .Matches(@"^[0-9+\-\s()]{6,40}$").WithMessage("Phone number is invalid.");
    }
}

public class UpdateGuestCommandHandler : IRequestHandler<UpdateGuestCommand, GuestDto>
{
    private readonly IUnitOfWork _uow;
    public UpdateGuestCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<GuestDto> Handle(UpdateGuestCommand request, CancellationToken ct)
    {
        var guests = _uow.Repository<Guest>();
        var guest = await guests.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Guest", request.Id);

        guest.FullName = request.FullName.Trim();
        guest.Email = request.Email.Trim();
        guest.Phone = request.Phone.Trim();
        guest.Nationality = request.Nationality?.Trim();
        guest.PassportNumber = request.PassportNumber?.Trim();

        guests.Update(guest);
        await _uow.SaveChangesAsync(ct);

        return guest.ToDto();
    }
}
