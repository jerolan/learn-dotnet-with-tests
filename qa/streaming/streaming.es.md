# Streaming con IAsyncEnumerable

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/streaming)

Un método que devuelve `Task<List<TaskItem>>` tiene que leer todas las filas antes de que el llamador vea la primera. Para mil tareas está bien. Para una exportación de un millón, la memoria crece con el resultado y el cliente espera al final para poder empezar.

`IAsyncEnumerable<T>` devuelve los elementos a medida que están disponibles. Este apéndice escribe una fuente que produce tareas, prueba la lectura completa, cancela a mitad y comprueba qué conserva el llamador cuando la fuente lanza después de haber producido algo.

## Cómo funciona

`IAsyncEnumerable<T>` entrega el control al llamador de un elemento en un momento. Los tests fijan lo que observa quien consume antes de implementar la fuente.

`yield return` suspende el método hasta que el llamador pide el siguiente elemento. `await Task.Yield()` representa trabajo asíncrono real, como una lectura de base o una llamada HTTP. El chequeo de cancelación corre antes de cada elemento, así un consumidor que deja de pedir no deja a la fuente procesando el resto.

`[EnumeratorCancellation]` permite que el token viaje con el enumerador cuando el llamador usa `WithCancellation`. Sin él, el token que recibe el método y el que se ata a la enumeración son distintos.

## Cómo probarlo

La fuente es determinista y está en memoria; no hay base de datos, servidor ni servicio externo. Ejecuta `dotnet test Streaming.sln` desde esta carpeta.

## Escribe primero el test

El primer test comprueba el contrato del consumidor: todos los elementos llegan en orden. Va antes de la implementación porque define lo que debe observar quien llama.

```csharp
[TestMethod]
public async Task ReadsEveryItemInOrder()
{
    var tasks = new[]
    {
        new TaskItem(Guid.NewGuid(), "First"),
        new TaskItem(Guid.NewGuid(), "Second"),
        new TaskItem(Guid.NewGuid(), "Third")
    };
    var source = new TaskSource(tasks);

    var seen = new List<TaskItem>();

    await foreach (var task in source.ReadAsync())
    {
        seen.Add(task);
    }

    CollectionAssert.AreEqual(tasks, seen);
}
```

La aserción falla porque reunió cero elementos en vez de tres.

`await foreach` es el lado consumidor de la misma forma. El test lleva el enumerador hasta el final y comprueba el orden.

## Intenta ejecutar el test

Antes de que exista `TaskSource.ReadAsync`, el compilador indica que falta `TaskSource`.

## Escribe el código mínimo para que el test compile y mira la salida

Este iterador vacío compila, pero no satisface el test:

```csharp
public async IAsyncEnumerable<TaskItem> ReadAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await Task.Yield();
    yield break;
}
```

## Escribe el código suficiente para que pase

`src/TaskStream/TaskSource.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace TaskStream;

public sealed class TaskSource
{
    private readonly IReadOnlyList<TaskItem> _tasks;

    public TaskSource(IReadOnlyList<TaskItem> tasks)
    {
        _tasks = tasks;
    }

    public async IAsyncEnumerable<TaskItem> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var task in _tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return task;
        }
    }
}
```

El iterador produce cada tarea y revisa la cancelación antes de avanzar a la siguiente. La suite completa pasa después de los tests adicionales.

## Refactor

El consumidor pide un elemento a la vez. Esperar el cuerpo del `await foreach` frena a la fuente; llamar `ToListAsync` o `ToArray` primero tira eso por la borda y bufferiza todo.

El enumerador vive lo que dura el loop. No devuelvas un iterador que captura un `DbContext` scoped y esperes que funcione después de que termine el scope de la petición. Materializa dentro del scope o mantén el stream dentro de él.

`IAsyncEnumerable` también es la forma en que EF Core expone una query sin cargarla: `AsAsyncEnumerable`. Úsalo cuando el llamador pueda procesar filas incrementalmente, no cuando el siguiente paso necesite el conjunto completo.

## Repite con requisitos nuevos

El siguiente requisito es dejar de producir elementos cuando el llamador cancela:

El segundo test cancela después del primer elemento:

```csharp
using var cancellation = new CancellationTokenSource();
var seen = new List<TaskItem>();

await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
{
    await foreach (var task in source.ReadAsync(cancellation.Token))
    {
        seen.Add(task);
        cancellation.Cancel();
    }
});

Assert.AreEqual(1, seen.Count);
```

Si la fuente ignora el token, el test lo dice:

```text
Assert.ThrowsException failed. No exception thrown. OperationCanceledException exception was expected.
```

El stream terminó normal porque nada revisó el token. Vuelve a poner `ThrowIfCancellationRequested` dentro del loop y la enumeración se detiene después del elemento que disparó la cancelación.

### Qué conserva el llamador cuando la fuente falla

El tercer test usa un iterador local que produce un valor y después lanza:

```csharp
private static async IAsyncEnumerable<int> FailsAfterOne()
{
    yield return 1;
    await Task.Yield();
    throw new InvalidOperationException("The source failed.");
}
```

```csharp
var seen = new List<int>();

await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
{
    await foreach (var item in FailsAfterOne())
    {
        seen.Add(item);
    }
});

CollectionAssert.AreEqual(new[] { 1 }, seen);
```

Los elementos ya producidos se entregaron; la excepción llega en el siguiente movimiento. Eso es distinto de un `Task<List<T>>` que falla antes de devolver nada. Quien consume un stream tiene que manejar un resultado parcial.

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 36 ms
```

### Streaming por HTTP

Una API mínima puede devolver `IAsyncEnumerable<T>` directamente:

```csharp
app.MapGet("/tasks/stream", (TaskSource source, CancellationToken cancellationToken) =>
    source.ReadAsync(cancellationToken));
```

`System.Text.Json` enumera la secuencia y escribe un arreglo JSON. Los elementos se escriben a medida que llegan, pero el resultado sigue siendo un solo documento JSON; un consumidor que necesita cada mensaje en su propia línea quiere NDJSON o server-sent events.

Para NDJSON, escribe un objeto serializado por línea y haz flush de la respuesta después de cada uno:

```csharp
await foreach (var task in source.ReadAsync(cancellationToken))
{
    await JsonSerializer.SerializeAsync(response.Body, task, cancellationToken);
    await response.WriteAsync("\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}
```

Server-sent events agrega el content type `text/event-stream` y un prefijo `data:` por mensaje. Los dos formatos empujan bytes antes de que la fuente termine; ninguno reemplaza una cola durable cuando un cliente desconectado no puede perder mensajes.

## Lo que queda diferido

No hay test de streaming HTTP, ni endpoint SSE, ni backpressure medido. Un laboratorio práctico expondría NDJSON, lo leería con `HttpCompletionOption.ResponseHeadersRead`, comprobaría que la primera línea llega antes de que la fuente termine y se desconectaría a mitad para demostrar que el servidor se detiene.

## Para cerrar

Lo que cubrimos:

- Un iterador asíncrono con `yield return` y `[EnumeratorCancellation]`.
- Una lectura completa, una cancelación y un fallo después de un resultado parcial.
- Las formas de arreglo JSON, NDJSON y SSE del lado HTTP.
- Backpressure, tiempo de vida del enumerador y `AsAsyncEnumerable` de EF Core.
- Los tests de HTTP y desconexión que necesita un endpoint de streaming real.

Sigue: despliegue en la nube.
