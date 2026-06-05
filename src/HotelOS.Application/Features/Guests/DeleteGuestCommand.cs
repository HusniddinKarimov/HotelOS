using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;

namespace HotelOS.Application.Features.Guests;

/// <summary>Permanently deletes a guest (Administrator only).</summary>
public record DeleteGuestCommand(Guid Id) : IRequest<Unit>;

public class DeleteGuestCommandHandler : IRequestHandler<DeleteGuestCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _audit;

    public DeleteGuestCommandHandler(IUnitOfWork uow, IAuditLogger audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<Unit> Handle(DeleteGuestCommand request, CancellationToken ct)
    {
        var guests = _uow.Repository<Guest>();
        var guest = await guests.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Guest", request.Id);

        // Preserve referential integrity and history: a guest with reservations
        // (and therefore bills/payments) cannot be hard-deleted.
        if (await _uow.Repository<Reservation>().AnyAsync(r => r.GuestId == request.Id, ct))
            throw new ConflictException("This guest has reservation history and cannot be deleted.");

        guests.Remove(guest);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("DeleteGuest", nameof(Guest), guest.Id.ToString(), guest.FullName, ct);

        return Unit.Value;
    }
}
