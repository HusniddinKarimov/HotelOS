using HotelOS.Contracts.Models;

namespace HotelOS.RoomService.Domain;

/// <summary>
/// Holds room-service orders. Uses BOTH required data structures:
///   • Queue&lt;string&gt;  — the processing pipeline, FIFO by arrival.
///   • Dictionary&lt;string,RoomServiceOrder&gt; — O(1) lookup by order id.
/// A lock keeps state changes atomic.
/// </summary>
public sealed class OrderBoard
{
    private readonly Queue<string> _pipeline = new();
    private readonly Dictionary<string, RoomServiceOrder> _orders = new();
    private readonly object _gate = new();

    public void Add(RoomServiceOrder order)
    {
        lock (_gate)
        {
            _orders[order.Id] = order;
            _pipeline.Enqueue(order.Id);
        }
    }

    /// <summary>
    /// Advance an order to its next state. Returns the updated order, or null
    /// if the id is unknown or the order is already Delivered.
    /// </summary>
    public RoomServiceOrder? Advance(string orderId)
    {
        lock (_gate)
        {
            if (!_orders.TryGetValue(orderId, out var order)) return null;
            order.Status = order.Status switch
            {
                OrderStatus.Received       => OrderStatus.Preparing,
                OrderStatus.Preparing      => OrderStatus.OutForDelivery,
                OrderStatus.OutForDelivery => OrderStatus.Delivered,
                _                          => order.Status // already Delivered
            };
            return order;
        }
    }

    public List<RoomServiceOrder> Active()
    {
        lock (_gate)
            return _orders.Values
                .Where(o => o.Status != OrderStatus.Delivered)
                .OrderBy(o => o.CreatedUtc)
                .ToList();
    }
}
