# Monitoreo y telemetría

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/monitoring-telemetry)

Los logs dicen qué pasó en una petición. Las métricas dicen con qué frecuencia pasa en todas las peticiones. Las trazas conectan las piezas de una petición entre servicios. Este apéndice agrega un contador con tags y lo prueba con un `MeterListener`.

El ejemplo registra pedidos creados, etiquetados por tipo de cliente.

## Escribe primero el test

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

## Agrega las métricas

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

Verde. El contador es un instrumento long; el tag forma parte de la medición, no del nombre de la métrica.

## Cómo funciona

Un `Meter` es una fuente nombrada de instrumentos. Un `Counter<long>` es monótonamente creciente; úsalo para eventos de "cuántos". Un `Histogram<double>` registra distribuciones como duraciones. Un `ObservableGauge` reporta un valor cuando el colector lo pide.

Un tag es un par clave/valor adjunto a una medición. El backend agrupa por tag: `orders.created{customer.type=premium}` y `orders.created{customer.type=standard}` son dos series de un mismo contador.

`MeterListener` se suscribe en proceso. Sirve para un test, un exportador propio o una herramienta de diagnóstico. Producción suele conectar un exportador de OpenTelemetry: el mismo `Meter` y `Counter` van a un colector, un tablero y una regla de alerta.

## Repite: dos mediciones

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

Verde. El valor de un contador en un backend es la suma de sus mediciones en la ventana de agregación.

## Refactor

Las métricas son un contrato público con un costo:

- Nombra los instrumentos por lo que miden e incluye la unidad: `orders.created` con `unit: "orders"`. Evita `count` solo; no dice nada.
- Mantén baja la cardinalidad. Un tag con id de usuario o email crea una serie por usuario y puede saturar un backend. Los datos de alta cardinalidad van en logs o trazas, con un id que puedas buscar.
- Usa contadores para eventos, histogramas para duraciones y tamaños, gauges para valores actuales. No codifiques una duración en el nombre de un contador.
- Mide lo que un operador puede accionar: tasa de peticiones, tasa de errores, latencia, profundidad de cola. Un tablero de interioridades del framework rara vez responde una pregunta de incidente.
- Correlaciona métricas con logs y trazas mediante el mismo nombre de servicio, entorno e id de correlación.
- Prueba el instrumento y el tag, como hace este apéndice. Una métrica con un nombre mal escrito es invisible hasta que alguien la necesita.

OpenTelemetry es el siguiente paso habitual:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("TaskTracker.Orders")
        .AddOtlpExporter());
```

El código de la aplicación sigue usando `Meter` y `Counter<T>`; solo cambia el exportador.

## Para cerrar

Lo que cubrimos:

- Un `Meter`, un `Counter<long>` y un tag.
- Un `MeterListener` que captura mediciones en un test.
- Contadores, histogramas y gauges como instrumentos distintos.
- Cardinalidad, nombres, unidades y el punto de vista del operador.
- OpenTelemetry como exportador de producción.

Siguiente: herramientas de testing de integración.

