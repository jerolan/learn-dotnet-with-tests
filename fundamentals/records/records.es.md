# Structs y records

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/records)

Una copia de un contador debe cambiar sin modificar el original. Dos records `Person` con el mismo nombre y edad deben compararse como iguales.

El primer ejemplo no compila porque `Counter` no existe:

```csharp
var original = new Counter { Value = 1 };
var copy = original;
copy.Increment();
```

Al ejecutar `dotnet test`, aparece `error CS0246: The type or namespace name 'Counter' could not be found`. Usaremos un test para ver cómo se copian los structs y después otros para ver cómo se comparan los records. La solución terminada está en `fundamentals/records`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Records`, un proyecto de MSTest en `tests/Records.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Un `struct` es un tipo por valor: asignarlo copia sus datos. Un `record` es un tipo por referencia con igualdad por valor: dos records se comparan como iguales cuando sus valores coinciden, aunque sean objetos distintos. Tanto los structs como los records pueden guardar datos, pero la asignación y la igualdad funcionan de forma distinta en cada caso.

## Cómo probarlo

Estos ejemplos se mantienen en memoria. Para un struct, modifica una copia y comprueba que el original no cambie. Para un record, crea dos instancias y compáralas con `Assert.AreEqual`. Esas aserciones hacen visibles la copia y la igualdad por valor en el punto de uso.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Records.Tests;

[TestClass]
public sealed class CounterTests
{
    [TestMethod]
    public void Assignment_CopiesTheStruct()
    {
        var original = new Counter { Value = 1 };
        var copy = original;

        copy.Increment();

        Assert.AreEqual(1, original.Value);
        Assert.AreEqual(2, copy.Value);
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0246: The type or namespace name 'Counter' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Records;

public struct Counter
{
    public int Value { get; set; }

    public void Increment()
    {
    }
}
```

El código compila. La primera aserción pasa porque el original sigue en `1`; la segunda falla con `Expected:<2>. Actual:<1>` porque `Increment` no hace nada.

## Escribe el código suficiente para que pase

```csharp
public void Increment()
{
    Value++;
}
```

Verde. Lo interesante es lo que no le pasó a `original`. `copy = original` copió los campos del struct, así que `copy` y `original` son dos valores separados. Llamar a `Increment` en la copia cambió solo la copia.

Eso es semántica de valores. Un struct se copia cuando se asigna, cuando se pasa a un método y cuando se devuelve desde uno. Es una buena opción para valores pequeños que se comportan como números. Los structs mutables son fáciles de usar mal, y por eso el código de producción suele declararlos `readonly`.

## Refactor

No hay nada que cambiar en este contador: es mutable a propósito para que el test muestre la copia. Si un valor no debe cambiar, usa `readonly struct` y deja que el compilador rechace las mutaciones.

## Repite con requisitos nuevos

### Un record compara por valor

Escribe el test:

```csharp
[TestClass]
public sealed class PersonTests
{
    [TestMethod]
    public void TwoPeopleWithTheSameValues_AreEqual()
    {
        Assert.AreEqual(new Person("Ada", 36), new Person("Ada", 36));
    }
}
```

Ejecuta los tests. El compilador dice que `Person` no existe. Define un record posicional:

```csharp
namespace Records;

public record Person(string Name, int Age);
```

Verde. `Person` es un tipo por referencia, pero el compilador le generó igualdad por valor: dos objetos distintos con el mismo `Name` y la misma `Age` son iguales. `Assert.AreEqual` usa `Equals`, que el record sobrescribe.

La sintaxis posicional también genera un constructor, propiedades de solo inicialización `Name` y `Age`, un `ToString` y soporte para deconstrucción. Las propiedades se pueden asignar en un inicializador de objeto, pero no después de construir el objeto.

### Valores distintos no son iguales

Escribe la otra mitad del contrato:

```csharp
[TestMethod]
public void TwoPeopleWithDifferentValues_AreNotEqual()
{
    Assert.AreNotEqual(new Person("Ada", 36), new Person("Grace", 36));
}
```

Pasa con el record tal como está. El test igual sirve: fija el otro lado de la igualdad y fallaría si un cambio posterior hiciera que todas las personas compararan iguales.

### Un record puede tener comportamiento

Escribe el test:

```csharp
[TestMethod]
public void IsAdult_ChecksTheAge()
{
    Assert.IsTrue(new Person("Ada", 36).IsAdult);
    Assert.IsFalse(new Person("Tim", 10).IsAdult);
}
```

El compilador dice que `Person` no tiene `IsAdult`. Agrega una propiedad calculada al record:

```csharp
public record Person(string Name, int Age)
{
    public bool IsAdult => Age >= 18;
}
```

Verde. Un record no es solo un saco de campos; puede tener propiedades y métodos como cualquier otro tipo. La diferencia está en la igualdad y en la sintaxis concisa.

### Mantener inmutables los valores del record

Las propiedades del record son de solo inicialización. Intentar asignar una después de construirlo es un error de compilación:

```csharp
var person = new Person("Ada", 36);
person.Age = 37;
// error CS8852: Init-only property or indexer 'Person.Age' can only be
// assigned in an object initializer, or in a constructor or init accessor
```

Eso prepara el siguiente capítulo. Para "cambiar" un record creas uno nuevo que comparte los valores sin cambios y reemplaza los que nombras.

Un `record struct` combina las dos ideas: copia por valor con la igualdad y la impresión generadas de un record. Úsalo para valores pequeños donde la copia es barata y quieres igualdad de record.

## Para cerrar

Lo que cubrimos:

- Un struct es un tipo por valor; asignarlo copia sus datos.
- Mutar una copia no cambia el original.
- Un record es un tipo por referencia con igualdad por valor.
- Los records posicionales generan constructor, propiedades de solo inicialización, igualdad e impresión.
- Un record puede tener propiedades calculadas y métodos.
- `readonly struct` y `record struct` como variantes por valor.

El capítulo sigue la sección de structs de [Structs, Methods and Interfaces en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces), donde un tipo agrupa datos y métodos. .NET agrega los records para datos que se comparan por valor y se mantienen inmutables.

Siguiente: [La palabra with](../with/with.es.md).

