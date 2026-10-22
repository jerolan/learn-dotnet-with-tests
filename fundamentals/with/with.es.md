# La palabra with

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/with)

Las propiedades de un record son de solo inicialización: una vez que el objeto existe, no puedes asignarlas. La expresión `with` es la forma de hacer una copia modificada. Se lee como una mutación y se comporta como una copia, que es lo que necesitan los datos inmutables.

La solución terminada está en `fundamentals/with`. Si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/WithKeyword`, un proyecto de MSTest en `tests/WithKeyword.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WithKeyword.Tests;

[TestClass]
public sealed class RecordCopyTests
{
    [TestMethod]
    public void With_CreatesAModifiedCopy()
    {
        var original = new Person("Ada", 36);

        var older = original with { Age = 37 };

        Assert.AreEqual(36, original.Age);
        Assert.AreEqual(37, older.Age);
        Assert.AreEqual("Ada", older.Name);
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0246: The type or namespace name 'Person' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace WithKeyword;

public record Person(string Name, int Age);
```

Ejecuta los tests. Verde.

Esta vez no hubo un rojo de comportamiento: `with` es una característica del lenguaje que funciona en cuanto el record existe. El test igual se ganó su lugar, porque fija el comportamiento que importa: el original conserva su edad y la copia tiene la nueva.

## Cómo funciona

`original with { Age = 37 }` hace tres cosas:

1. Copia cada propiedad de `original` en un record nuevo.
2. Aplica las asignaciones del inicializador. Aquí, `Age` pasa a `37`.
3. Devuelve el record nuevo.

`original` queda intacto. La expresión no muta el objeto; construye un segundo. `older` y `original` son objetos distintos, y los records comparan por valor, así que no son iguales porque sus edades difieren.

Puedes cambiar más de una propiedad en el mismo inicializador:

```csharp
var renamed = original with { Name = "Grace", Age = 37 };
```

Las propiedades que no nombras se copian sin cambios.

## Repite con requisitos nuevos

### Un inicializador vacío crea una copia igual

Escribe el test:

```csharp
[TestMethod]
public void With_AnEmptyInitializer_CreatesAnEqualCopy()
{
    var original = new Person("Ada", 36);

    var copy = original with { };

    Assert.AreEqual(original, copy);
}
```

Pasa. Un `with` vacío copia todo y no cambia nada, y la igualdad de records dice que la copia es igual al original. Es un borde útil: separa "mismos valores" de "mismo objeto".

### Records anidados

Los datos reales suelen estar anidados. Escribe el test:

```csharp
[TestMethod]
public void With_NestedRecord_UpdatesOnlyTheInnerValue()
{
    var customer = new Customer("Ada", new Address("London"));

    var moved = customer with { Home = customer.Home with { City = "Paris" } };

    Assert.AreEqual("London", customer.Home.City);
    Assert.AreEqual("Paris", moved.Home.City);
    Assert.AreEqual("Ada", moved.Name);
}
```

Ejecútalo. El compilador dice que `Customer` no existe. Agrega los records:

```csharp
public record Address(string City);

public record Customer(string Name, Address Home);
```

Verde. El `with` interno crea una dirección nueva, y el `with` externo crea un cliente nuevo que apunta a la dirección nueva. El cliente original sigue apuntando a la dirección de Londres.

Si escribieras solo `customer with { Home = new Address("Paris") }`, reemplazarías la dirección completa. La forma anidada sirve cuando la dirección tiene más campos y quieres cambiar uno.

## Refactor

`with` hace una copia superficial. Copia los valores de las propiedades, y para una propiedad de tipo referencia el valor es la referencia. Si un record guarda una lista mutable, los dos records ven los cambios hechos a través de esa lista:

```csharp
public record Team(string Name, List<string> Members);

var team = new Team("A", new List<string> { "Ada" });
var copy = team with { Name = "B" };

copy.Members.Add("Grace");

// team.Members ahora también tiene dos entradas
```

No es un error de `with`; es lo que significa "copiar la referencia". Si la colección tiene que ser independiente, cópiala explícitamente:

```csharp
var copy = team with { Name = "B", Members = new List<string>(team.Members) };
```

La misma regla aplica a cualquier objeto mutable dentro de un record. Mantén los records superficiales y sus miembros inmutables cuando puedas.

`with` funciona con records y, desde C# 10, con structs. Conserva el tipo en tiempo de ejecución: un `with` sobre un record derivado devuelve el tipo derivado, con las propiedades que nombraste.

## Para cerrar

Lo que cubrimos:

- `with` como actualización no destructiva para records.
- El objeto original queda intacto y el resultado es un objeto nuevo.
- Un inicializador vacío devuelve una copia igual.
- Expresiones `with` anidadas para cambios dentro de un record anidado.
- Semántica de copia superficial y cómo copiar explícitamente una colección mutable.

El capítulo se apoya en la semántica de valores de [Structs and Records en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): los datos se mantienen inmutables y cada cambio produce un valor nuevo. La expresión `with` es la sintaxis de C# para esa idea.

Siguiente: [Herencia e interfaces](../inheritance/inheritance.es.md).

