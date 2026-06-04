using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Guests;

/// <summary>Fetches a guest with their full reservation history.</summary>
public record GetGuestByIdQuery(Guid Id) : IRequest<GuestDetailDto>;

public class GetGuestByIdQueryHandler : IRequestHandler<GetGuestByIdQuery, GuestDetailDto>
{
    private readonly IUnitOfWork _uow;
    public GetGuestByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<GuestDetailDto> Handle(GetGuestByIdQuery request, CancellationToken ct)
    {
        var guest = await _uow.Repository<Guest>().Query()
            .Include(g => g.Reservations).ThenInclude(r => r.RoomType)
            .Include(g => g.Reservations).ThenInclude(r => r.Room)
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("Guest", request.Id);

        var history = guest.Reservations
            .OrderByDescending(r => r.CheckInDate)
            .Select(r => new GuestReservationSummary(
                r.Id, r.ReferenceCode, r.RoomType?.Name ?? string.Empty,
                r.Room?.Number, r.CheckInDate, r.CheckOutDate, r.Status.ToString()))
            .ToList();

        return new GuestDetailDto(
            guest.Id, guest.FullName, guest.Email, guest.Phone,
            guest.Nationality, guest.PassportNumber, guest.CreatedAt, history);
    }
}
