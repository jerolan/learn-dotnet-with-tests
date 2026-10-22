# Async/Await

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/async)

Si una fuente del clima devuelve una temperatura de forma asíncrona, `WeatherReport.DescribeAsync("Madrid")` debe devolver `"Madrid: 20C"` sin bloquear mientras trabaja la fuente. La solución terminada está en `fundamentals/async`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Async`, un proyecto de MSTest en `tests/Async.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Un método `async` devuelve una `Task` o `Task<T>`. En `await`, devuelve el control a quien lo llamó mientras la tarea esperada siga incompleta y continúa cuando esa tarea termina. Quien llama puede esperar la tarea sin bloquear un hilo.

## Cómo probarlo

Separa el reporte del clima del efecto secundario que obtiene la temperatura mediante `ITemperatureSource`. Una fuente falsa puede devolver una tarea completada, así que el test comprueba el resultado formateado sin demoras, acceso a la red ni suposiciones de tiempo. El test devuelve `async Task` y espera el reporte.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Async.Tests;

[TestClass]
public sealed class WeatherReportTests
{
    [TestMethod]
    public async Task DescribeAsync_ReturnsTheFormattedReport()
    {
        var source = new FakeSource((_, _) => Task.FromResult(20));
        var report = new WeatherReport(source);

        Assert.AreEqual("Madrid: 20C", await report.DescribeAsync("Madrid"));
    }
}
```

El fake devuelve una tarea completada. Sin demora, sin red y sin suposiciones de tiempo.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'WeatherReport' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Async;

public interface ITemperatureSource
{
    Task<int> GetAsync(string city, CancellationToken cancellationToken);
}

public sealed class WeatherReport
{
    private readonly ITemperatureSource _source;

    public WeatherReport(ITemperatureSource source)
    {
        _source = source;
    }

    public Task<string> DescribeAsync(string city, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Empty);
    }
}
```

El test compila y falla con `Expected:<Madrid: 20C>. Actual:<>`.

## Escribe el código suficiente para que pase

```csharp
public async Task<string> DescribeAsync(string city, CancellationToken cancellationToken = default)
{
    var temperature = await _source.GetAsync(city, cancellationToken);

    return $"{city}: {temperature}C";
}
```

Verde. El método lleva `async` y devuelve `Task<string>`. Adentro, `await` desarrolla la `Task<int>` de la fuente en un `int`. Quien llama recibe una tarea que completa con la cadena formateada.

## Refactor

El método ya tiene la forma que quieres: async de punta a punta, un await, sin bloqueos.

Algunas reglas que evitan la mayoría de los errores asíncronos:

- No llames a `.Result` ni `.Wait()` sobre una tarea. Usa `await`. Bloquear puede producir un deadlock y envuelve las excepciones en `AggregateException`.
- Usa `async Task` para métodos asíncronos. `async void` es solo para manejadores de eventos; sus excepciones no las puede observar quien llama.
- No envuelvas E/S naturalmente asíncrona en `Task.Run`. La API de E/S ya devuelve una tarea. `Task.Run` es para trabajo ligado a CPU que debe salir del hilo actual.
- Pasa `CancellationToken` por toda la cadena. Un token que se detiene en el método público no le sirve a la base de datos ni a la llamada HTTP de abajo.
- En código de biblioteca, `ConfigureAwait(false)` evita capturar el contexto de sincronización de quien llama. En código de aplicación con contexto de UI, el valor por defecto suele ser el correcto. El libro usa el valor por defecto para no distraer.

## Repite con requisitos nuevos

### Excepciones de la fuente

Escribe el test:

```csharp
[TestMethod]
public async Task DescribeAsync_PropagatesTheSourceException()
{
    var source = new FakeSource((_, _) =>
        Task.FromException<int>(new InvalidOperationException("offline")));
    var report = new WeatherReport(source);

    await Assert.ThrowsExceptionAsync<InvalidOperationException>(
        () => report.DescribeAsync("Madrid"));
}
```

Pasa. `Task.FromException<int>` crea una tarea fallida. Cuando el método la espera, la `InvalidOperationException` se lanza dentro del método y se propaga a quien llama.

Ese es el contrato asíncrono: una excepción en una tarea esperada aparece en el `await`, no como un `AggregateException` de `.Result`. Si el método no la atrapa, la tarea devuelta queda fallida con esa excepción.

### Cancelación

Escribe el test:

```csharp
[TestMethod]
public async Task DescribeAsync_CanceledToken_Throws()
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var source = new FakeSource((_, token) =>
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(20);
    });
    var report = new WeatherReport(source);

    await Assert.ThrowsExceptionAsync<OperationCanceledException>(
        () => report.DescribeAsync("Madrid", cts.Token));
}
```

Pasa. El método pasa el token de cancelación a la fuente; la fuente llama a `ThrowIfCancellationRequested` y lanza `OperationCanceledException`, que el método propaga.

La cancelación es cooperativa. Un token no detiene un hilo; el código que lo recibe tiene que comprobarlo o pasarlo a una API que lo haga. Pasa el token hasta la llamada de E/S y deja que la cancelación llegue a la operación que puede honrarla.

## Para cerrar

Lo que cubrimos:

- Métodos `async` que devuelven `Task` o `Task<T>`.
- `await` para desarrollar una tarea sin bloquear el hilo.
- Métodos de test `async Task`.
- Propagación de excepciones desde una tarea esperada.
- `CancellationToken` y cancelación cooperativa.
- Los hábitos que evitan deadlocks por `.Result`, sorpresas de `async void` y `Task.Run` innecesario.

El capítulo sigue la idea de E/S asíncrona del [capítulo Concurrency de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/concurrency): el trabajo corre por separado y quien llama espera un resultado. .NET lo convierte en `Task` y `await`.

Siguiente: [Archivos e I/O](../io/io.es.md).

