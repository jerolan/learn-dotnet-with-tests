namespace Shop;

public sealed record Order(
    string CustomerName,
    string Email,
    decimal Total,
    DateTime PlacedAt);
