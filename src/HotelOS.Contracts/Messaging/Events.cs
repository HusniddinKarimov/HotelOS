using HotelOS.Contracts.Models;

namespace HotelOS.Contracts.Messaging;

// These records are the payload contracts carried by each topic. They are
// deliberately minimal: NO card numbers, passport numbers or other sensitive
// guest data is ever placed on the broker (see Task 3 — Data exposure).

/// <summary>reception.room_assigned — a guest was placed in a room.</summary>
public record RoomAssignedEvent(int RoomNumber, string GuestId, string GuestName, RoomType Type, int Floor);

/// <summary>reception.room_vacated — a room is now empty and needs cleaning.</summary>
public record RoomVacatedEvent(int RoomNumber);

/// <summary>reception.checked_out — billing finished for a stay.</summary>
public record CheckedOutEvent(int RoomNumber, string GuestName, decimal Total);

/// <summary>housekeeping.status_changed — a room's clean-state moved on.</summary>
public record HousekeepingStatusEvent(int RoomNumber, RoomStatus Status);

/// <summary>roomservice.order_update — an order changed state.</summary>
public record OrderUpdateEvent(string OrderId, int RoomNumber, string Summary, OrderStatus Status);

/// <summary>roomservice.charge — a charge to post against a room's bill.</summary>
public record OrderChargeEvent(int RoomNumber, string Description, decimal Amount);

/// <summary>maintenance.issue_update — a maintenance issue changed state.</summary>
public record MaintenanceUpdateEvent(string IssueId, int RoomNumber, string Description, Urgency Urgency, IssueStatus Status, string? Technician);
