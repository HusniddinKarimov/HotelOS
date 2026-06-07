using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.SelfService;

/// <summary>All of the signed-in guest's bookings, newest first.</summary>
public record GetMyBookingsQuery : IRequest<IReadOnlyList<BookingDto>>;

public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, IReadOnlyList<BookingDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetMyBookingsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<BookingDto>> Handle(GetMyBookingsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var today = DateTime.UtcNow.Date;

        var reservations = await _uow.Repository<Reservation>().Query()
            .Include(r => r.Room)
            .Include(r => r.RoomType)
            .Include(r => r.Bill)
            .Where(r => r.BookedByUserId == userId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync(ct);

        return reservations.Select(r => new BookingDto(
            r.Id, r.ReferenceCode, r.Room?.Number, r.RoomType.Name,
            r.CheckInDate, r.CheckOutDate, r.Nights, r.Status.ToString(),
            r.Bill?.Total ?? 0m, r.Bill?.Status == BillStatus.Paid,
            CanCheckIn: r.Status == ReservationStatus.Confirmed && r.CheckInDate.Date <= today))
            .ToList();
    }
}
