using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Rooms;

/// <summary>Fetches a single room by id.</summary>
public record GetRoomByIdQuery(Guid Id) : IRequest<RoomDto>;

public class GetRoomByIdQueryHandler : IRequestHandler<GetRoomByIdQuery, RoomDto>
{
    private readonly IUnitOfWork _uow;
    public GetRoomByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<RoomDto> Handle(GetRoomByIdQuery request, CancellationToken ct)
    {
        var room = await _uow.Repository<Room>().Query()
            .Include(r => r.RoomType)
            .Include(r => r.CurrentGuest)
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Room", request.Id);

        return room.ToDto();
    }
}
