# Genéricos

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/generics)

Un tipo o método genérico recibe un tipo como parámetro. Escribes el algoritmo una vez, el compilador lo comprueba con cada tipo que usas, y conservas la seguridad de tipos en vez de convertir a `object`.

Vamos a construir un repositorio que guarda entidades por id y funciona con cualquier tipo de entidad. La solución terminada está en `fundamentals/generics`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Generics`, un proyecto de MSTest en `tests/Generics.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Generics.Tests;

[TestClass]
public sealed class RepositoryTests
{
    [TestMethod]
    public void AddThenGet_ReturnsTheItem()
    {
        var repository = new Repository<Product>();
        var product = new Product(1, "Keyboard");

        repository.Add(product);

        Assert.AreEqual(product, repository.Get(1));
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Repository' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Empieza por el contrato que el repositorio necesita de un elemento, una entidad y un repositorio genérico que compile:

```csharp
namespace Generics;

public interface IEntity
{
    int Id { get; }
}

public sealed record Product(int Id, string Name) : IEntity;

public sealed class Repository<T> where T : class, IEntity
{
    public void Add(T item)
    {
    }

    public T? Get(int id)
    {
        return null;
    }
}
```

El test compila y falla con `Expected:<Product { Id = 1, Name = Keyboard }>. Actual:<null>`.

## Escribe el código suficiente para que pase

```csharp
public sealed class Repository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public void Add(T item)
    {
        _items[item.Id] = item;
    }

    public T? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

Verde. `Repository<T>` guarda valores de `T` en un diccionario con clave `int`. El diccionario también es genérico: `Dictionary<int, T>`.

## Cómo funciona

`T` es el parámetro de tipo. `new Repository<Product>()` lo cierra con `Product`; `new Repository<Customer>()` lo cierra con `Customer`. Cada tipo cerrado es un tipo distinto para el compilador, y los dos comparten el mismo código fuente.

La cláusula `where T : class, IEntity` es una restricción. `class` dice que `T` es un tipo por referencia, lo que permite devolver `T?` como null. `IEntity` dice que cada `T` tiene una propiedad `Id`, que es lo que usa `Add`. Sin la restricción, el compilador rechazaría `item.Id`: un `T` cualquiera no tiene miembros.

El repositorio nunca menciona `Product` ni `Customer`. Solo conoce la interfaz que exige. Ese es el punto de la restricción: describe el mínimo que necesita el algoritmo.

## Repite con requisitos nuevos

### Un id que no existe

Escribe el test:

```csharp
[TestMethod]
public void GetMissing_ReturnsNull()
{
    var repository = new Repository<Product>();

    Assert.IsNull(repository.Get(99));
}
```

Pasa con la implementación actual porque `TryGetValue` devuelve `false` y el método devuelve null. El test fija ese contrato: un id ausente no es una excepción aquí.

### Todos los elementos

Escribe el test:

```csharp
[TestMethod]
public void All_ReturnsEveryItem()
{
    var repository = new Repository<Product>();
    repository.Add(new Product(1, "Keyboard"));
    repository.Add(new Product(2, "Mouse"));

    var all = new List<Product>(repository.All);

    Assert.AreEqual(2, all.Count);
}
```

El compilador dice que falta `All`. Agrega una vista de solo lectura:

```csharp
public IReadOnlyCollection<T> All => _items.Values;
```

Verde. `Dictionary<int, T>.Values` implementa `IReadOnlyCollection<T>`, y la propiedad la expone sin dejar que quien llama agregue o quite entradas.

### Un segundo tipo de entidad

Escribe el test:

```csharp
[TestMethod]
public void WorksWithAnotherEntityType()
{
    var repository = new Repository<Customer>();
    repository.Add(new Customer(7, "Ada"));

    Assert.AreEqual("Ada", repository.Get(7)!.Name);
}
```

Agrega la entidad:

```csharp
public sealed record Customer(int Id, string Name) : IEntity;
```

Verde sin tocar `Repository<T>`. Esa es la recompensa: una implementación, dos tipos de entidad, y el compilador comprobó los dos.

## Refactor

El repositorio es pequeño. Las decisiones de diseño son la restricción y el diccionario.

La restricción es deliberadamente estrecha. Pide `Id` y nada más. Una restricción más amplia, como una clase base con muchos miembros, ataría el repositorio a más de lo que necesita y lo haría más difícil de reutilizar.

Los genéricos no son lo mismo que `object`. Una `List<object>` aceptaría cualquier cosa, obligaría a convertir al sacar los elementos y haría boxing de los tipos por valor. `List<T>` mantiene el tipo del elemento en la firma y deja que el compilador rechace una asignación incorrecta.

Si más adelante el repositorio tiene que reemplazarse en los tests, extrae `IRepository<T>` e inyéctalo. El parámetro de tipo va también en la interfaz:

```csharp
public interface IRepository<T> where T : class, IEntity
{
    void Add(T item);

    T? Get(int id);
}
```

## Para cerrar

Lo que cubrimos:

- Una clase genérica con un parámetro de tipo, `Repository<T>`.
- Una restricción, `where T : class, IEntity`, que documenta lo que el código necesita.
- Un diccionario genérico, `Dictionary<int, T>`.
- Un retorno nullable, `T?`, y un contrato para el id ausente.
- Una vista de colección de solo lectura.
- La misma implementación usada con dos tipos de entidad.

El capítulo sigue el [capítulo Generics de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/generics): una función o tipo que trabaja con varios tipos de elemento mientras el compilador mantiene los tipos honestos. .NET agrega restricciones y varianza sobre la misma idea.

Siguiente: [La palabra ref](../ref.md).

