using System.Diagnostics.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OrderMetrics.Tests;

[TestClass]
public sealed class OrderServiceTests
{
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

    [TestMethod]
    public void TwoCreates_RecordTwoMeasurements()
    {
        using var metrics = new OrderMetrics("TaskTracker.Orders.Count.Tests");
        var service = new OrderService(metrics);
        using var listener = new MeterListener();
        var total = 0L;

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter == metrics.Meter)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
            total += measurement);

        listener.Start();

        service.Create("standard");
        service.Create("standard");

        Assert.AreEqual(2, total);
    }
}

