using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reservations;

/// <summary>Fetches a single reservation by id.</summary>
public record GetReservationByIdQuery(Guid Id) : IRequest<ReservationDto>;

public class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, ReservationDto>
{
    private readonly IUnitOfWork _uow;
    public GetReservationByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReservationDto> Handle(GetReservationByIdQuery request, CancellationToken ct)
    {
        var reservation = await _uow.Repository<Reservation>().Query()
            .Include(r => r.Guest)
            .Include(r => r.RoomType)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Reservation", request.Id);

        return reservation.ToDto();
    }
}
