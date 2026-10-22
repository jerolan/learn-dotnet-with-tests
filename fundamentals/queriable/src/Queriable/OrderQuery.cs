namespace Queriable;

public sealed record Order(string Customer, decimal Total);

public sealed class OrderRepository
{
    private readonly IEnumerable<Order> _orders;

    public OrderRepository(IEnumerable<Order> orders)
    {
        _orders = orders;
    }

    public IQueryable<Order> Query()
    {
        return _orders.AsQueryable();
    }
}

public static class OrderService
{
    public static IQueryable<Order> Expensive(IQueryable<Order> orders, decimal minimum)
    {
        return orders.Where(order => order.Total >= minimum);
    }
}

