using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Maintenance;

/// <summary>Logs a maintenance fault and places it in the priority queue.</summary>
public record CreateMaintenanceRequestCommand(int RoomNumber, string Description, string Priority)
    : IRequest<MaintenanceDto>;

public class CreateMaintenanceRequestCommandValidator : AbstractValidator<CreateMaintenanceRequestCommand>
{
    public CreateMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.RoomNumber).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority).Must(p => Enum.TryParse<MaintenancePriority>(p, true, out _))
            .WithMessage("Priority must be Critical, High, Normal or Low.");
    }
}

public class CreateMaintenanceRequestCommandHandler : IRequestHandler<CreateMaintenanceRequestCommand, MaintenanceDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public CreateMaintenanceRequestCommandHandler(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<MaintenanceDto> Handle(CreateMaintenanceRequestCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<MaintenanceRequest>();
        var nextSequence = (await repo.Query().Select(m => (long?)m.Sequence).MaxAsync(ct) ?? 0) + 1;

        var entity = new MaintenanceRequest
        {
            RoomNumber = request.RoomNumber,
            Description = request.Description.Trim(),
            Priority = Enum.Parse<MaintenancePriority>(request.Priority, true),
            Status = MaintenanceStatus.Open,
            Sequence = nextSequence
        };

        await repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        await _realtime.NotifyAsync(NotificationType.MaintenanceRequest,
            $"{entity.Priority} maintenance reported for room {entity.RoomNumber}.",
            targetRole: RoleNames.MaintenanceStaff, ct: ct);
        await _realtime.ActivityAsync($"Maintenance ({entity.Priority}) logged for room {entity.RoomNumber}.", ct);

        return entity.ToDto();
    }
}
