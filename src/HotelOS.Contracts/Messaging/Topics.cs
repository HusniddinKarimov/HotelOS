namespace HotelOS.Contracts.Messaging;

/// <summary>
/// The complete catalogue of event topics that flow through the broker.
/// Centralising these as constants prevents typos and acts as the single
/// source of truth documented in the event table (Task 3).
/// </summary>
public static class Topics
{
    // Published by Reception
    public const string RoomAssigned = "reception.room_assigned";
    public const string RoomVacated  = "reception.room_vacated";
    public const string CheckedOut   = "reception.checked_out";

    // Published by Housekeeping
    public const string HousekeepingStatusChanged = "housekeeping.status_changed";

    // Published by Room Service
    public const string OrderUpdate = "roomservice.order_update";
    public const string OrderCharge = "roomservice.charge";

    // Published by Maintenance
    public const string MaintenanceUpdate = "maintenance.issue_update";

    /// <summary>Wildcard used by the dashboard to receive every event.</summary>
    public const string All = "*";
}
