namespace HotelOS.Domain.Common;

/// <summary>Marks an entity that carries audit timestamps set by the DbContext.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

/// <summary>Base class for aggregate roots keyed by a Guid, with audit fields.</summary>
public abstract class BaseEntity : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
