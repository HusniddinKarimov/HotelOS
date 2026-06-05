namespace HotelOS.Application.Features.SelfService;

public record MenuItem(string Name, decimal Price);

/// <summary>
/// The fixed room-service menu. Prices live on the server so a guest can never
/// tamper with them when ordering.
/// </summary>
public static class Menu
{
    public static readonly IReadOnlyList<MenuItem> Items = new[]
    {
        new MenuItem("Coffee", 3.5m),
        new MenuItem("Tea", 3.0m),
        new MenuItem("Bottled Water", 2.0m),
        new MenuItem("Club Sandwich", 9.0m),
        new MenuItem("Cheeseburger", 12.5m),
        new MenuItem("Caesar Salad", 8.5m),
        new MenuItem("Margherita Pizza", 11.0m),
        new MenuItem("Chocolate Cake", 5.5m),
    };

    public static decimal? PriceOf(string name) =>
        Items.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))?.Price;
}
