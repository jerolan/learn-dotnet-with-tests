# La palabra ref

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/ref)

Un parámetro normal es una copia. `ref` pasa una referencia a la variable de quien llama, `out` declara un valor que el método debe producir, y `in` pasa una referencia de solo lectura. Bien usados evitan copias; usados sin cuidado vuelven el código más difícil de seguir.

Vamos a cubrir las cuatro formas con ejemplos pequeños y testeables: intercambiar dos variables, intentar dividir, devolver una referencia a una posición de un arreglo y pasar un struct por referencia de solo lectura. La solución terminada está en `fundamentals/ref`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Ref`, un proyecto de MSTest en `tests/Ref.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ref.Tests;

[TestClass]
public sealed class ValuesTests
{
    [TestMethod]
    public void Swap_ExchangesTheValues()
    {
        var left = 1;
        var right = 2;

        Values.Swap(ref left, ref right);

        Assert.AreEqual(2, left);
        Assert.AreEqual(1, right);
    }
}
```

## Intenta ejecutar el test

```text
error CS0103: The name 'Values' does not exist in the current context
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Ref;

public static class Values
{
    public static void Swap(ref int left, ref int right)
    {
    }
}
```

El test compila y falla porque las dos variables conservan sus valores originales. El método recibió referencias pero no hizo nada con ellas.

## Escribe el código suficiente para que pase

```csharp
public static void Swap(ref int left, ref int right)
{
    var temp = left;
    left = right;
    right = temp;
}
```

Verde. `ref` en el parámetro significa que el método trabaja sobre las variables de quien llama, no sobre copias. Sin él, `left` y `right` dentro del método serían copias independientes y quien llama no vería ningún cambio.

## Repite con requisitos nuevos

### Un parámetro de salida

Escribe el test:

```csharp
[TestMethod]
public void TryDivide_ByDivisor_ReturnsTheQuotient()
{
    var result = Values.TryDivide(10, 2, out var quotient);

    Assert.IsTrue(result);
    Assert.AreEqual(5, quotient);
}
```

Agrega el método con un parámetro `out`:

```csharp
public static bool TryDivide(int dividend, int divisor, out int quotient)
{
    quotient = dividend / divisor;
    return true;
}
```

Verde para el caso válido. Ahora el divisor cero:

```csharp
[TestMethod]
public void TryDivide_ByZero_ReturnsFalseAndZero()
{
    var result = Values.TryDivide(10, 0, out var quotient);

    Assert.IsFalse(result);
    Assert.AreEqual(0, quotient);
}
```

Maneja el guard:

```csharp
public static bool TryDivide(int dividend, int divisor, out int quotient)
{
    if (divisor == 0)
    {
        quotient = 0;
        return false;
    }

    quotient = dividend / divisor;
    return true;
}
```

Verde. Un parámetro `out` debe asignarse antes de que el método retorne; el compilador rechaza un camino que lo deje sin asignar. Quien llama no necesita inicializarlo: `out var quotient` lo declara en el lugar de la llamada.

### Un retorno por referencia

Escribe el test:

```csharp
[TestMethod]
public void At_ReturnsARefToTheSlot()
{
    var values = new[] { 1, 2, 3 };

    ref var slot = ref Values.At(values, 1);
    slot = 20;

    Assert.AreEqual(20, values[1]);
}
```

Agrega el método:

```csharp
public static ref int At(int[] values, int index)
{
    return ref values[index];
}
```

Verde. `ref int` es un retorno por referencia: el método devuelve una referencia al elemento del arreglo, no una copia de su valor. `ref var slot = ref ...` es una variable local por referencia. Asignar `20` a `slot` escribe en el arreglo.

Un retorno por referencia debe apuntar a una ubicación que sobreviva a la llamada, como un elemento de un arreglo o un campo. Devolver una referencia a una variable local dejaría una referencia colgada, y el compilador lo rechaza.

### Una referencia de solo lectura

Escribe el test:

```csharp
[TestMethod]
public void ManhattanDistance_AddsTheCoordinateDifferences()
{
    var origin = new Point(0, 0);

    Assert.AreEqual(7, origin.ManhattanDistance(new Point(3, 4)));
}
```

Agrega el struct de solo lectura y el parámetro `in`:

```csharp
public readonly struct Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int ManhattanDistance(in Point other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }
}
```

Verde. `in` pasa una referencia de solo lectura. El método evita copiar el struct y el compilador le impide asignar a `other`. Para un struct grande en una ruta caliente, eso importa; para un punto de dos enteros, es sobre todo una lección de sintaxis. Quien llama puede pasar el argumento con o sin la palabra `in`.

## Refactor

Los ejemplos son pequeños a propósito. La regla práctica es usar estas palabras solo cuando compran algo:

- `ref` cuando el método tiene que mutar la variable de quien llama, como en `Swap`.
- `out` para un método que devuelve éxito más un valor, el patrón `Try...`.
- `in` para un struct grande de solo lectura que no quieres copiar.
- Un retorno por referencia cuando quien llama necesita escribir en una posición, como un arreglo o un campo.

Para valores normales, devuelve un valor nuevo. Los datos inmutables y los tipos de retorno claros son más fáciles de razonar que un método que muta varios argumentos.

## Para cerrar

Lo que cubrimos:

- Parámetros `ref` y mutación de las variables de quien llama.
- Parámetros `out` y el patrón `Try...`.
- Retornos por referencia y variables locales por referencia.
- `in` para una referencia de solo lectura a un struct.
- Cuándo ayudan las palabras y cuándo un retorno normal es más claro.

El capítulo sigue el comportamiento de referencias de [Pointers en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors): una función puede trabajar sobre el valor de quien llama en vez de una copia. C# hace explícita la elección con `ref`, `out` e `in` en lugar de exponer sintaxis de punteros.

Siguiente: [Tasks](../tasks.md).

