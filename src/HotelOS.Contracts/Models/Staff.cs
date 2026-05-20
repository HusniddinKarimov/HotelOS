namespace HotelOS.Contracts.Models;

/// <summary>
/// Base class for every member of hotel staff. Demonstrates INHERITANCE:
/// shared identity/behaviour lives here and is reused by every department.
/// <see cref="RoleTitle"/> is abstract, forcing each subclass to supply its
/// own implementation (POLYMORPHISM).
/// </summary>
public abstract class StaffMember
{
    public string Id { get; }
    public string Name { get; }

    protected StaffMember(string id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Polymorphic: each role describes itself differently.</summary>
    public abstract string RoleTitle { get; }

    /// <summary>A human-readable label combining name and role.</summary>
    public virtual string Describe() => $"{Name} ({RoleTitle})";
}

/// <summary>Front-desk staff who run check-in / check-out.</summary>
public sealed class Receptionist : StaffMember
{
    public Receptionist(string id, string name) : base(id, name) { }
    public override string RoleTitle => "Receptionist";
}

/// <summary>Staff who clean rooms.</summary>
public sealed class Housekeeper : StaffMember
{
    public Housekeeper(string id, string name) : base(id, name) { }
    public override string RoleTitle => "Housekeeper";
}

/// <summary>
/// Maintenance technician. Adds availability state used by the priority-queue
/// assignment algorithm — only an available technician can be assigned a job.
/// </summary>
public sealed class Technician : StaffMember
{
    public Technician(string id, string name) : base(id, name) { }
    public override string RoleTitle => "Technician";

    public bool IsAvailable { get; set; } = true;

    /// <summary>Overrides the base description to show live availability.</summary>
    public override string Describe() =>
        $"{base.Describe()} — {(IsAvailable ? "available" : "busy")}";
}
