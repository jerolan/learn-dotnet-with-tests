# Monitoring and Telemetry

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/monitoring-telemetry)

Logs tell you what happened in one request. Metrics tell you how often it happens across all requests. Traces connect the pieces of one request across services. This appendix adds a counter with tags and tests it with a `MeterListener`.

The sample records created orders, tagged by customer type.

## Write the test first

```csharp
[TestMethod]
public void Create_RecordsACounterWithTheCustomerTag()
{
    using var metrics = new OrderMetrics("TaskTracker.Orders.Tests");
    var service = new OrderService(metrics);
    using var listener = new MeterListener();
    var measurements = new List<(long Value, string? CustomerType)>();

    listener.InstrumentPublished = (instrument, meterListener) =>
    {
        if (instrument.Meter == metrics.Meter)
        {
            meterListener.EnableMeasurementEvents(instrument);
        }
    };

    listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) =>
    {
        string? customerType = null;

        foreach (var tag in tags)
        {
            if (tag.Key == "customer.type")
            {
                customerType = tag.Value?.ToString();
            }
        }

        measurements.Add((measurement, customerType));
    });

    listener.Start();

    service.Create("premium");

    Assert.AreEqual(1, measurements.Count);
    Assert.AreEqual(1, measurements[0].Value);
    Assert.AreEqual("premium", measurements[0].CustomerType);
}
```

## Add the metrics

```csharp
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
```

Green. The counter is a long instrument; the tag is part of the measurement, not part of the metric name.

## How it works

A `Meter` is a named source of instruments. A `Counter<long>` is monotonically increasing; use it for "how many" events. `Histogram<double>` records distributions such as durations. `ObservableGauge` reports a value when the collector asks.

A tag is a key/value pair attached to one measurement. The metric backend groups by tag: `orders.created{customer.type=premium}` and `orders.created{customer.type=standard}` are two series from one counter.

`MeterListener` subscribes in process. It is useful for a test, a custom exporter or a diagnostic tool. Production usually wires an OpenTelemetry exporter: the same `Meter` and `Counter` go to a collector, a dashboard and an alerting rule.

## Repeat: two measurements

```csharp
[TestMethod]
public void TwoCreates_RecordTwoMeasurements()
{
    using var metrics = new OrderMetrics("TaskTracker.Orders.Count.Tests");
    var service = new OrderService(metrics);
    using var listener = new MeterListener();
    var total = 0L;

    listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        total += measurement);

    listener.Start();

    service.Create("standard");
    service.Create("standard");

    Assert.AreEqual(2, total);
}
```

Green. A counter's value in a backend is the sum of its measurements over the aggregation window.

## Refactor

Metrics are a public contract with a cost:

- Name instruments after the thing measured and include the unit: `orders.created` with `unit: "orders"`. Avoid `count` alone; it says nothing.
- Keep cardinality low. A tag with a user id or an email creates one series per user and can overwhelm a backend. High-cardinality data belongs in logs or traces, with an id you can search.
- Use counters for events, histograms for durations and sizes, gauges for current values. Do not encode a duration in a counter name.
- Measure what an operator can act on: request rate, error rate, latency, queue depth. A dashboard of framework internals rarely answers an incident question.
- Correlate metrics with logs and traces through the same service name, environment and correlation id.
- Test the instrument and the tag, as this appendix does. A metric with a typo in its name is invisible until someone needs it.

OpenTelemetry is the usual next step:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("TaskTracker.Orders")
        .AddOtlpExporter());
```

The application code keeps using `Meter` and `Counter<T>`; only the exporter changes.

## Wrapping up

What we have covered:

- A `Meter`, a `Counter<long>` and a tag.
- A `MeterListener` that captures measurements in a test.
- Counters, histograms and gauges as different instruments.
- Cardinality, naming, units and the operator's point of view.
- OpenTelemetry as the production exporter.

Next: integration testing tools.

