using FluentValidation;
using HotelOS.Application.Abstractions;
using HotelOS.Application.Common;
using HotelOS.Application.Features.Maintenance;
using HotelOS.Application.Features.Orders;
using MediatR;

namespace HotelOS.Application.Features.SelfService;

public record OrderLine(string Name, int Quantity);

/// <summary>The signed-in guest orders room service for their own room (menu-priced).</summary>
public record PlaceMyOrderCommand(IReadOnlyList<OrderLine> Items) : IRequest<OrderDto>;

/// <summary>The signed-in guest reports a maintenance issue for their own room.</summary>
public record ReportMyIssueCommand(string Description, string Priority) : IRequest<MaintenanceDto>;

public class PlaceMyOrderCommandValidator : AbstractValidator<PlaceMyOrderCommand>
{
    public PlaceMyOrderCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Select at least one item.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Name).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class ReportMyIssueCommandValidator : AbstractValidator<ReportMyIssueCommand>
{
    public ReportMyIssueCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Priority)
            .Must(p => p is "Low" or "Normal" or "High" or "Critical")
            .WithMessage("Priority must be Low, Normal, High or Critical.");
    }
}

public class PlaceMyOrderCommandHandler : IRequestHandler<PlaceMyOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _mediator;

    public PlaceMyOrderCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, ISender mediator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<OrderDto> Handle(PlaceMyOrderCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var roomNumber = await SelfServiceHelpers.CurrentRoomNumberAsync(_uow, userId, ct)
            ?? throw new ConflictException("Book a room before ordering room service.");

        // Resolve each item's price from the server-side menu (no client prices trusted).
        var items = request.Items.Select(line =>
        {
            var price = Menu.PriceOf(line.Name)
                ?? throw new ConflictException($"'{line.Name}' is not on the menu.");
            return new NewOrderItem(line.Name, line.Quantity, price);
        }).ToList();

        // Reuse the room-service flow: creates the order, bills it and notifies the kitchen.
        return await _mediator.Send(new CreateOrderCommand(roomNumber, items), ct);
    }
}

public class ReportMyIssueCommandHandler : IRequestHandler<ReportMyIssueCommand, MaintenanceDto>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _mediator;

    public ReportMyIssueCommandHandler(IUnitOfWork uow, ICurrentUser currentUser, ISender mediator)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<MaintenanceDto> Handle(ReportMyIssueCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new AuthenticationException("Not signed in.");
        var roomNumber = await SelfServiceHelpers.CurrentRoomNumberAsync(_uow, userId, ct)
            ?? throw new ConflictException("Book a room before reporting an issue.");

        // Reuse the maintenance flow: queues the request and notifies technicians.
        return await _mediator.Send(new CreateMaintenanceRequestCommand(roomNumber, request.Description, request.Priority), ct);
    }
}
