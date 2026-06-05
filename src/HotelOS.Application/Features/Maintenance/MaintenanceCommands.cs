using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Maintenance;

/// <summary>Assigns a maintenance request to a technician and marks the room under maintenance.</summary>
public record AssignMaintenanceCommand(Guid Id, Guid TechnicianUserId) : IRequest<MaintenanceDto>;

/// <summary>Marks a maintenance request complete and restores the room.</summary>
public record ResolveMaintenanceCommand(Guid Id) : IRequest<MaintenanceDto>;

public class AssignMaintenanceCommandHandler : IRequestHandler<AssignMaintenanceCommand, MaintenanceDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public AssignMaintenanceCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<MaintenanceDto> Handle(AssignMaintenanceCommand request, CancellationToken ct)
    {
        var request_ = await _uow.Repository<MaintenanceRequest>().Query(tracking: true)
            .Include(m => m.AssignedTo)
            .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
            ?? throw new NotFoundException("MaintenanceRequest", request.Id);

        if (request_.Status == MaintenanceStatus.Completed)
            throw new ConflictException("This request is already completed.");

        var technician = await _uow.Repository<User>().Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == request.TechnicianUserId, ct)
            ?? throw new NotFoundException("User", request.TechnicianUserId);

        request_.AssignedToUserId = technician.Id;
        request_.AssignedTo = technician;
        request_.Status = MaintenanceStatus.InProgress;

        // Take the room out of service if it is not currently occupied.
        var room = await _uow.Repository<Room>().Query(tracking: true)
            .FirstOrDefaultAsync(r => r.Number == request_.RoomNumber, ct);
        if (room is not null && room.Status != RoomStatus.Occupied)
            room.Status = RoomStatus.Maintenance;

        await _uow.SaveChangesAsync(ct);
        await _realtime.ActivityAsync($"Maintenance for room {request_.RoomNumber} assigned to {technician.FullName}.", ct);

        return request_.ToDto();
    }
}

public class ResolveMaintenanceCommandHandler : IRequestHandler<ResolveMaintenanceCommand, MaintenanceDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public ResolveMaintenanceCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<MaintenanceDto> Handle(ResolveMaintenanceCommand request, CancellationToken ct)
    {
        var entity = await _uow.Repository<MaintenanceRequest>().Query(tracking: true)
            .Include(m => m.AssignedTo)
            .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
            ?? throw new NotFoundException("MaintenanceRequest", request.Id);

        if (entity.Status == MaintenanceStatus.Completed)
            throw new ConflictException("This request is already completed.");

        entity.Status = MaintenanceStatus.Completed;
        entity.ResolvedAt = DateTime.UtcNow;

        var room = await _uow.Repository<Room>().Query(tracking: true)
            .FirstOrDefaultAsync(r => r.Number == entity.RoomNumber, ct);
        if (room is not null && room.Status == RoomStatus.Maintenance)
        {
            room.Status = RoomStatus.Clean;
            room.LastCleanedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);
        await _realtime.ActivityAsync($"Maintenance for room {entity.RoomNumber} resolved.", ct);

        return entity.ToDto();
    }
}
