using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Rooms;

/// <summary>Sets a room's status directly (admin/manager override).</summary>
public record UpdateRoomStatusCommand(Guid RoomId, RoomStatus Status) : IRequest<RoomDto>;

public class UpdateRoomStatusCommandHandler : IRequestHandler<UpdateRoomStatusCommand, RoomDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public UpdateRoomStatusCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<RoomDto> Handle(UpdateRoomStatusCommand request, CancellationToken ct)
    {
        var room = await _uow.Repository<Room>().Query(tracking: true)
            .Include(r => r.RoomType)
            .Include(r => r.CurrentGuest)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, ct)
            ?? throw new NotFoundException("Room", request.RoomId);

        room.Status = request.Status;
        if (request.Status == RoomStatus.Clean)
            room.LastCleanedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        await _realtime.ActivityAsync($"Room {room.Number} status set to {request.Status}.", ct);

        return room.ToDto();
    }
}
