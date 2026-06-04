using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Dashboard;

/// <summary>Aggregate metrics shown on the real-time operations dashboard.</summary>
public record DashboardDto(
    int TotalRooms,
    int AvailableRooms,
    int OccupiedRooms,
    int DirtyRooms,
    int CleaningRooms,
    int MaintenanceRooms,
    int ActiveGuests,
    int ActiveOrders,
    int OpenMaintenanceRequests,
    decimal Revenue);

public record GetDashboardQuery : IRequest<DashboardDto>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IUnitOfWork _uow;
    public GetDashboardQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var rooms = _uow.Repository<Room>().Query();

        var totalRooms = await rooms.CountAsync(ct);
        var available = await rooms.CountAsync(r => r.Status == RoomStatus.Clean || r.Status == RoomStatus.Available, ct);
        var occupied = await rooms.CountAsync(r => r.Status == RoomStatus.Occupied, ct);
        var dirty = await rooms.CountAsync(r => r.Status == RoomStatus.Dirty, ct);
        var cleaning = await rooms.CountAsync(r => r.Status == RoomStatus.Cleaning, ct);
        var maintenance = await rooms.CountAsync(r => r.Status == RoomStatus.Maintenance, ct);

        var activeGuests = await _uow.Repository<Reservation>().Query()
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn, ct);

        var activeOrders = await _uow.Repository<RoomServiceOrder>().Query()
            .CountAsync(o => o.Status != OrderStatus.Delivered, ct);

        var openMaintenance = await _uow.Repository<MaintenanceRequest>().Query()
            .CountAsync(m => m.Status != MaintenanceStatus.Completed, ct);

        var revenue = await _uow.Repository<Payment>().Query()
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        return new DashboardDto(totalRooms, available, occupied, dirty, cleaning, maintenance,
            activeGuests, activeOrders, openMaintenance, revenue);
    }
}
