# IEnumerable

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/enumerable)

`IEnumerable<T>` es el contrato de una secuencia: algo que un `foreach` puede recorrer elemento por elemento. Los arreglos y las listas lo implementan, y un método puede producir una secuencia de forma perezosa con `yield return`. La parte perezosa es lo que hace posibles las secuencias infinitas.

Vamos a construir tres secuencias: los números naturales, los pares hasta un límite, y una secuencia que registra cuándo se ejecuta. La solución terminada está en `fundamentals/enumerable`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Enumerable`, un proyecto de MSTest en `tests/Enumerable.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Enumerable.Tests;

[TestClass]
public sealed class SequenceTests
{
    [TestMethod]
    public void Naturals_FirstThreeAreZeroOneTwo()
    {
        var first = new List<int>();

        foreach (var number in Sequence.Naturals())
        {
            first.Add(number);

            if (first.Count == 3)
            {
                break;
            }
        }

        Assert.AreEqual(3, first.Count);
        Assert.AreEqual(0, first[0]);
        Assert.AreEqual(1, first[1]);
        Assert.AreEqual(2, first[2]);
    }
}
```

El `break` es parte de la especificación. La secuencia es infinita; el test toma solo lo que necesita.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Sequence' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Enumerable;

public static class Sequence
{
    public static IEnumerable<int> Naturals()
    {
        yield break;
    }
}
```

El código compila y el test falla con `Expected:<3>. Actual:<0>`. El método devuelve una secuencia vacía.

## Escribe el código suficiente para que pase

```csharp
public static IEnumerable<int> Naturals()
{
    var current = 0;

    while (true)
    {
        yield return current;
        current++;
    }
}
```

Verde. `yield return` produce un valor y pausa el método; el siguiente `MoveNext` continúa después de esa línea. El `while (true)` correría para siempre si quien llama siguiera pidiendo, pero el `break` del test se detiene en el tercer valor.

## Cómo funciona

`foreach` no sabe nada de `yield`. Llama a `GetEnumerator()` sobre la secuencia, después a `MoveNext()` y `Current` hasta que `MoveNext()` devuelve `false`, y al final libera el enumerador. El compilador convierte el método con `yield return` en una clase que implementa ese protocolo.

El cuerpo del método no corre cuando llamas a `Sequence.Naturals()`. Corre cuando empieza la enumeración. Cada llamada a `Naturals()` crea un enumerable nuevo; cada `foreach` arranca la secuencia desde el principio.

## Repite con requisitos nuevos

### Una secuencia finita

Escribe el test:

```csharp
[TestMethod]
public void Evens_UpToTen_ReturnsTheEvenNumbers()
{
    var evens = new List<int>();

    foreach (var number in Sequence.Evens(10))
    {
        evens.Add(number);
    }

    CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8, 10 }, evens);
}
```

Agrega el método con un cuerpo vacío primero:

```csharp
public static IEnumerable<int> Evens(int max)
{
    yield break;
}
```

El test falla con una colección vacía. Implementa:

```csharp
public static IEnumerable<int> Evens(int max)
{
    for (var i = 0; i <= max; i += 2)
    {
        yield return i;
    }
}
```

Verde. El mismo `yield return` funciona con un bucle finito. El método produce valores solo cuando quien llama los pide.

### Ejecución diferida

Escribe el test:

```csharp
[TestMethod]
public void Tracked_DoesNotRunUntilEnumerated()
{
    var log = new List<int>();
    var sequence = Sequence.Tracked(log);

    Assert.AreEqual(0, log.Count);

    using var enumerator = sequence.GetEnumerator();

    Assert.AreEqual(0, log.Count);

    enumerator.MoveNext();

    Assert.AreEqual(1, log.Count);
    Assert.AreEqual(1, enumerator.Current);
}
```

Agrega el método con un marcador, mira fallar el test y después implementa:

```csharp
public static IEnumerable<int> Tracked(List<int> log)
{
    log.Add(1);
    yield return 1;

    log.Add(2);
    yield return 2;
}
```

Verde. Las dos primeras aserciones demuestran que crear la secuencia y obtener el enumerador no ejecutan el cuerpo. El primer `MoveNext()` lo ejecuta hasta el primer `yield return`, y por eso el log tiene una entrada.

Si enumeras `Tracked` dos veces, el log recibe las entradas otra vez. La secuencia no es una lista cacheada; es una receta que corre cada vez.

## Refactor

Los tres métodos devuelven `IEnumerable<int>`, no una colección concreta. Ese es el contrato más pequeño que necesita quien llama: puede iterar, y no puede asumir índices ni mutación.

Algunos hábitos hacen las secuencias más seguras:

- Nunca devuelvas `null` por una secuencia. Devuelve `Array.Empty<T>()` o `Enumerable.Empty<T>()`, así quien llama siempre puede iterar.
- Ten en cuenta cuántas veces se enumera una secuencia. Una secuencia perezosa con efectos secundarios los repite en cada pasada.
- Si quien llama necesita acceso por índice o una foto estable, materializa la secuencia en una lista en el borde.
- Las secuencias infinitas necesitan un consumidor que se detenga, como el `break` del primer test. El `Take` de LINQ, que llega en un capítulo posterior, es la forma habitual de expresar ese límite.

## Para cerrar

Lo que cubrimos:

- `IEnumerable<T>` como contrato de iteración.
- `yield return` y `yield break`.
- El protocolo `GetEnumerator`/`MoveNext`/`Current` detrás de `foreach`.
- Una secuencia infinita consumida con un `break`.
- Una secuencia finita construida con un bucle `for`.
- Ejecución diferida y el hecho de que cada enumeración vuelve a empezar.

El capítulo sigue la disciplina de bucles del [capítulo Iteration de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/iteration): una secuencia se produce paso a paso y quien llama decide cuándo parar. La máquina de estados de `yield` es la forma de .NET para expresarlo.

Siguiente: [IQueryable](../queriable.md).

