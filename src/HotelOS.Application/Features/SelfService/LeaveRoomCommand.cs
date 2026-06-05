using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Billing;
using HotelOS.Application.Features.Reservations;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>
/// The signed-in user leaves their room. This checks them out, which frees the
/// room to Dirty automatically and queues it for housekeeping. Returns the bill.
/// </summary>
public record LeaveRoomCommand : IRequest<BillDto>;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand, BillDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _mediator;

    public LeaveRoomCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, ISender mediator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<BillDto> Handle(LeaveRoomCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");

        var reservation = await _uow.Repository<Reservation>().Query()
            .FirstOrDefaultAsync(r => r.BookedByUserId == userId && r.Status == ReservationStatus.CheckedIn, ct)
            ?? throw new ConflictException("You do not currently have a room.");

        // Reuse the check-out flow: it frees the room to Dirty and queues cleaning.
        return await _mediator.Send(new CheckOutCommand(reservation.Id), ct);
    }
}
