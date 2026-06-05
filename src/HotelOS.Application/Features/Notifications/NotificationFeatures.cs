using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Notifications;

public record NotificationDto(Guid Id, string Type, string Message, bool IsRead, DateTime CreatedAt);

/// <summary>The current user's notifications: those addressed to them, their role, or broadcast.</summary>
public class GetNotificationsQuery : PagedQueryBase, IRequest<PagedResult<NotificationDto>>
{
    public bool UnreadOnly { get; set; }
}

public record MarkNotificationReadCommand(Guid Id) : IRequest<Unit>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsQueryHandler(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var role = _currentUser.Role;
        var userId = _currentUser.UserId;

        var query = _uow.Repository<Notification>().Query()
            .Where(n => n.UserId == null && n.TargetRole == null      // broadcast
                     || (userId != null && n.UserId == userId)        // direct
                     || (role != null && n.TargetRole == role));      // role-scoped

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        query = query.OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync(ct);
        var list = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = list
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Message, n.IsRead, n.CreatedAt))
            .ToList();
        return new PagedResult<NotificationDto>(items, request.Page, request.PageSize, total);
    }
}

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    public MarkNotificationReadCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var n = await _uow.Repository<Notification>().GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Notification", request.Id);
        n.IsRead = true;
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
