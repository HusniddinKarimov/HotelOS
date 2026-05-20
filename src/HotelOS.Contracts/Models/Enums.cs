namespace HotelOS.Contracts.Models;

/// <summary>
/// The category of room a guest can book. Used by the room assignment
/// algorithm to match a booking to physical inventory.
/// </summary>
public enum RoomType
{
    Single,
    Double,
    Suite,
    Accessible
}

/// <summary>
/// The lifecycle status of a physical room. Only <see cref="Clean"/> rooms
/// are eligible for assignment. Transitions are driven by events flowing
/// through the message broker between Reception and Housekeeping.
/// </summary>
public enum RoomStatus
{
    Clean,
    Occupied,
    Dirty,
    BeingCleaned,
    Maintenance
}

/// <summary>The state machine a room-service order moves through.</summary>
public enum OrderStatus
{
    Received,
    Preparing,
    OutForDelivery,
    Delivered
}

/// <summary>
/// Maintenance urgency. The integer value is the priority weight used by the
/// priority queue: lower value = higher priority (Critical is served first).
/// </summary>
public enum Urgency
{
    Critical = 0,
    High = 1,
    Normal = 2,
    Low = 3
}

/// <summary>The lifecycle status of a maintenance issue.</summary>
public enum IssueStatus
{
    Open,
    Assigned,
    Resolved
}
