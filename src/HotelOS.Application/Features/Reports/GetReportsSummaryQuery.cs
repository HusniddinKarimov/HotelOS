using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Application.Features.Reports;

public record ReportsSummaryDto(
    decimal RevenueTotal,
    IReadOnlyDictionary<string, decimal> RevenueByMethod,
    int TotalReservations,
    int CheckedInNow,
    int TotalRooms,
    int OccupiedRooms,
    double OccupancyRate,
    IReadOnlyDictionary<string, int> RoomsByStatus);

public record GetReportsSummaryQuery : IRequest<ReportsSummaryDto>;

public class GetReportsSummaryQueryHandler : IRequestHandler<GetReportsSummaryQuery, ReportsSummaryDto>
{
    private readonly IUnitOfWork _uow;
    public GetReportsSummaryQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportsSummaryDto> Handle(GetReportsSummaryQuery request, CancellationToken ct)
    {
        var payments = await _uow.Repository<Payment>().Query()
            .Where(p => p.Status == PaymentStatus.Completed)
            .ToListAsync(ct);

        var revenueByMethod = payments
            .GroupBy(p => p.Method.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var rooms = await _uow.Repository<Room>().Query().ToListAsync(ct);
        var roomsByStatus = rooms
            .GroupBy(r => r.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var occupied = rooms.Count(r => r.Status == RoomStatus.Occupied);
        var totalReservations = await _uow.Repository<Reservation>().Query().CountAsync(ct);
        var checkedIn = await _uow.Repository<Reservation>().Query()
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn, ct);

        return new ReportsSummaryDto(
            payments.Sum(p => p.Amount),
            revenueByMethod,
            totalReservations,
            checkedIn,
            rooms.Count,
            occupied,
            rooms.Count == 0 ? 0 : Math.Round(occupied * 100.0 / rooms.Count, 1),
            roomsByStatus);
    }
}
