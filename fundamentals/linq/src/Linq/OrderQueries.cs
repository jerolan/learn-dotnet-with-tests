namespace Linq;

public sealed record Order(string Customer, decimal Total);

public static class OrderQueries
{
    public static decimal TotalFor(IEnumerable<Order> orders, string customer)
    {
        return orders
            .Where(order => order.Customer == customer)
            .Sum(order => order.Total);
    }

    public static IReadOnlyList<string> TopCustomers(IEnumerable<Order> orders, int count)
    {
        return orders
            .GroupBy(order => order.Customer)
            .OrderByDescending(group => group.Sum(order => order.Total))
            .ThenBy(group => group.Key)
            .Take(count)
            .Select(group => group.Key)
            .ToList();
    }

    public static bool HasOrdersOver(IEnumerable<Order> orders, decimal amount)
    {
        return orders.Any(order => order.Total > amount);
    }
}

