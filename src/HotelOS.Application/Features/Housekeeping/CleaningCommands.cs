using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Housekeeping;

/// <summary>Housekeeper begins cleaning a room (Dirty -> Cleaning).</summary>
public record StartCleaningCommand(Guid TaskId) : IRequest<HousekeepingTaskDto>;

/// <summary>Housekeeper finishes cleaning a room (Cleaning -> Clean).</summary>
public record CompleteCleaningCommand(Guid TaskId) : IRequest<HousekeepingTaskDto>;

public class StartCleaningCommandHandler : IRequestHandler<StartCleaningCommand, HousekeepingTaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public StartCleaningCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<HousekeepingTaskDto> Handle(StartCleaningCommand request, CancellationToken ct)
    {
        var task = await _uow.Repository<HousekeepingTask>().Query(tracking: true)
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct)
            ?? throw new NotFoundException("HousekeepingTask", request.TaskId);

        if (task.Status == HousekeepingStatus.Completed)
            throw new ConflictException("This cleaning task is already completed.");

        task.Status = HousekeepingStatus.InProgress;
        task.StartedAt = DateTime.UtcNow;
        if (task.Room is { } room) room.Status = RoomStatus.Cleaning;

        await _uow.SaveChangesAsync(ct);
        await _realtime.ActivityAsync($"Room {task.RoomNumber} is being cleaned.", ct);

        return task.ToDto();
    }
}

public class CompleteCleaningCommandHandler : IRequestHandler<CompleteCleaningCommand, HousekeepingTaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public CompleteCleaningCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<HousekeepingTaskDto> Handle(CompleteCleaningCommand request, CancellationToken ct)
    {
        var task = await _uow.Repository<HousekeepingTask>().Query(tracking: true)
            .Include(t => t.Room)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct)
            ?? throw new NotFoundException("HousekeepingTask", request.TaskId);

        if (task.Status == HousekeepingStatus.Completed)
            throw new ConflictException("This cleaning task is already completed.");

        task.Status = HousekeepingStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;
        if (task.Room is { } room)
        {
            room.Status = RoomStatus.Clean;
            room.LastCleanedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);
        await _realtime.NotifyAsync(NotificationType.CleaningCompleted, $"Room {task.RoomNumber} is now clean and available.", ct: ct);
        await _realtime.ActivityAsync($"Room {task.RoomNumber} cleaned and available.", ct);

        return task.ToDto();
    }
}
