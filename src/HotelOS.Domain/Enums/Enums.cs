namespace HotelOS.Domain.Enums;

/// <summary>Lifecycle status of a physical room.</summary>
public enum RoomStatus
{
    Available,
    Reserved,
    Occupied,
    Dirty,
    Cleaning,
    Clean,
    Maintenance
}

/// <summary>Lifecycle of a reservation.</summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled
}

/// <summary>State machine for a room-service order.</summary>
public enum OrderStatus
{
    Received,
    Preparing,
    Ready,
    Delivering,
    Delivered
}

/// <summary>Cleaning task lifecycle.</summary>
public enum HousekeepingStatus
{
    Pending,
    InProgress,
    Completed
}

/// <summary>Maintenance urgency. Lower value = higher priority.</summary>
public enum MaintenancePriority
{
    Critical = 0,
    High = 1,
    Normal = 2,
    Low = 3
}

/// <summary>Maintenance request lifecycle.</summary>
public enum MaintenanceStatus
{
    Open,
    Assigned,
    InProgress,
    Completed
}

/// <summary>Billing line categories.</summary>
public enum BillItemType
{
    Room,
    Food,
    MiniBar,
    LateCheckout,
    Discount,
    Other
}

/// <summary>Invoice/bill lifecycle.</summary>
public enum BillStatus
{
    Open,
    Paid,
    Cancelled
}

/// <summary>Accepted payment methods.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer
}

/// <summary>Payment lifecycle.</summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

/// <summary>Notification categories raised by domain events.</summary>
public enum NotificationType
{
    CheckIn,
    CheckOut,
    CleaningCompleted,
    NewOrder,
    MaintenanceRequest,
    PaymentCompleted,
    System
}
