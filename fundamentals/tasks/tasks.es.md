# Tasks

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/tasks)

Una `Task` es un identificador de trabajo que puede terminar más tarde. `Task<T>` agrega un resultado. Puedes esperar una tarea, esperar a todas o quedarte con la primera que termine. Los tests pueden controlar cuándo completa una tarea, así que nada de esto necesita una demora real.

Vamos a construir dos combinadores: sumar los resultados de varias tareas y devolver el primero. La solución terminada está en `fundamentals/tasks`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Tasks`, un proyecto de MSTest en `tests/Tasks.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Una `Task` representa trabajo que puede terminar más tarde; `Task<T>` también lleva un resultado. `await` deja que el método continúe cuando termina una tarea sin bloquear el hilo. `Task.WhenAll` espera todas las entradas y `Task.WhenAny` completa cuando termina la primera.

## Cómo probarlo

Usa `Task.FromResult` para trabajo ya completado y `TaskCompletionSource<T>` cuando el test necesite controlar la finalización. Así los tests son deterministas y evitan demoras reales.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tasks.Tests;

[TestClass]
public sealed class TaskCombinatorsTests
{
    [TestMethod]
    public async Task SumAsync_AddsCompletedTasks()
    {
        var sum = await TaskCombinators.SumAsync(
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3));

        Assert.AreEqual(6, sum);
    }
}
```

El método de test devuelve `Task`, no `void`. MSTest lo espera, así que un fallo dentro del test asíncrono se reporta como cualquier otro.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'TaskCombinators' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Tasks;

public static class TaskCombinators
{
    public static Task<int> SumAsync(params Task<int>[] tasks)
    {
        return Task.FromResult(0);
    }
}
```

El test compila y falla con `Expected:<6>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public static async Task<int> SumAsync(params Task<int>[] tasks)
{
    var values = await Task.WhenAll(tasks);

    var sum = 0;

    foreach (var value in values)
    {
        sum += value;
    }

    return sum;
}
```

Verde. `Task.WhenAll(tasks)` devuelve una tarea que completa cuando completan todas las entradas, con un `int[]` de sus resultados. `await` desarrolla ese arreglo.

La palabra `async` permite usar `await` en el método y devolver `int` aunque la firma diga `Task<int>`. Quien llama ve una tarea; el resultado aparece cuando la tarea completa.

## Refactor

La suma espera todas las tareas y combina sus resultados sin bloquear. Ejecuta el test otra vez; ya es la implementación clara más corta.

## Repite con requisitos nuevos

### Esperar a todas las tareas

Escribe el test:

```csharp
[TestMethod]
public async Task SumAsync_WaitsForEveryTask()
{
    var first = new TaskCompletionSource<int>();
    var second = new TaskCompletionSource<int>();

    var sumTask = TaskCombinators.SumAsync(first.Task, second.Task);

    Assert.IsFalse(sumTask.IsCompleted);

    first.SetResult(1);

    Assert.IsFalse(sumTask.IsCompleted);

    second.SetResult(2);

    Assert.AreEqual(3, await sumTask);
}
```

Pasa. `TaskCompletionSource<int>` le da al test una tarea que puede completar a mano. Después del primer resultado, la tarea de suma sigue incompleta; después del segundo, termina. Sin `Task.Delay` y sin suposiciones de tiempo.

Ese es el test que quieres para "esperar a todas": falla si la implementación retorna después de la primera tarea.

### La primera tarea en terminar

Escribe el test:

```csharp
[TestMethod]
public async Task FirstAsync_ReturnsTheFirstCompletedTask()
{
    var slow = new TaskCompletionSource<int>();
    var fast = new TaskCompletionSource<int>();

    var firstTask = TaskCombinators.FirstAsync(slow.Task, fast.Task);

    fast.SetResult(20);

    Assert.AreEqual(20, await firstTask);
}
```

Agrega el método:

```csharp
public static async Task<int> FirstAsync(params Task<int>[] tasks)
{
    var completed = await Task.WhenAny(tasks);

    return await completed;
}
```

Verde. `Task.WhenAny` devuelve la tarea que terminó primero; el segundo `await` desarrolla su resultado. La tarea lenta sigue pendiente y no se espera, así que su resultado se ignora.

### Sin tareas

Escribe el borde:

```csharp
[TestMethod]
public async Task SumAsync_WithNoTasks_ReturnsZero()
{
    Assert.AreEqual(0, await TaskCombinators.SumAsync());
}
```

Pasa. `Task.WhenAll` sobre un arreglo vacío devuelve un arreglo vacío, y la suma empieza en cero.

### Mantener asíncrono el código no bloqueante

Los combinadores son cortos; los hábitos alrededor importan.

No bloquees una tarea con `.Result` ni `.Wait()`. En una aplicación de UI o una petición de ASP.NET, bloquear un hilo mientras espera trabajo que necesita ese mismo hilo puede producir un deadlock. Además envuelve los fallos en un `AggregateException` en vez de dejar que `await` muestre la excepción original.

Usa `async Task` para métodos asíncronos. `async void` es solo para manejadores de eventos: quien llama no puede esperarlo, y una excepción lanzada dentro no se observa de la forma normal.

`Task.Run` es para trabajo ligado a CPU que quieres mover fuera del hilo actual. El trabajo de E/S, como una llamada a base de datos o a HTTP, ya es asíncrono; envolverlo en `Task.Run` solo agrega un salto de hilo.

`Task.WhenAll` recoge los fallos de varias tareas. Esperar la tarea combinada lanza una excepción, y las demás quedan disponibles en la propiedad `Exception` de la tarea. El siguiente capítulo trata excepciones y cancelación con más detalle.

## Para cerrar

Lo que cubrimos:

- `Task` y `Task<T>` como identificadores de trabajo en curso.
- Métodos de test `async` y `await`.
- `Task.WhenAll` y su arreglo de resultados.
- `Task.WhenAny` para la primera tarea completada.
- `TaskCompletionSource<T>` para tests deterministas.
- El borde de entrada vacía.
- Por qué evitar `.Result`, `.Wait()`, `async void` y el `Task.Run` innecesario.

El capítulo sigue los objetivos de concurrencia del [capítulo Concurrency de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/concurrency): varias piezas de trabajo corren por separado y quien llama decide cómo combinarlas. .NET expresa la combinación con `Task.WhenAll` y `Task.WhenAny`.

Siguiente: [Async/Await](../async/async.es.md).

