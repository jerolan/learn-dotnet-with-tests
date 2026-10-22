using System.Diagnostics.Metrics;

namespace OrderMetrics;

public sealed class OrderMetrics : IDisposable
{
    public OrderMetrics(string name = "TaskTracker.Orders")
    {
        Meter = new Meter(name, "1.0");
        OrdersCreated = Meter.CreateCounter<long>(
            "orders.created",
            unit: "orders",
            description: "Number of orders created.");
    }

    public Meter Meter { get; }

    public Counter<long> OrdersCreated { get; }

    public void Dispose()
    {
        Meter.Dispose();
    }
}

public sealed class OrderService
{
    private readonly OrderMetrics _metrics;

    public OrderService(OrderMetrics metrics)
    {
        _metrics = metrics;
    }

    public void Create(string customerType)
    {
        _metrics.OrdersCreated.Add(
            1,
            new KeyValuePair<string, object?>("customer.type", customerType));
    }
}

