# Genéricos

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/generics)

Agregar un producto a un repositorio y buscarlo por id debe devolver el mismo producto. Cuando agreguemos clientes, el repositorio debe servir para ambos tipos sin aceptar objetos que no correspondan.

La primera llamada no compila porque `ProductRepository` no existe:

```csharp
var repository = new ProductRepository();
repository.Add(new Product(1, "Keyboard"));
```

Al ejecutar `dotnet test`, aparece `error CS0246: The type or namespace name 'ProductRepository' could not be found`. Empezaremos con productos, agregaremos otro tipo de entidad y después refactorizaremos el algoritmo repetido en un repositorio genérico. La solución terminada está en `fundamentals/generics`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Generics`, un proyecto de MSTest en `tests/Generics.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Un repositorio guarda entidades por id. Un `Dictionary<int, Product>` puede encontrar un producto por su `Id`. Cuando los tests muestren que productos y clientes necesitan las mismas operaciones, un parámetro de tipo permitirá que una implementación de repositorio sirva para ambos y que el compilador siga conociendo el tipo de entidad.

## Cómo probarlo

El repositorio guarda los datos en memoria. Un test unitario puede agregar un producto, recuperarlo por id y comparar directamente el resultado. Después probaremos un id ausente, la colección completa y el mismo comportamiento con un cliente.

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
        var repository = new ProductRepository();
        var product = new Product(1, "Keyboard");

        repository.Add(product);

        Assert.AreEqual(product, repository.Get(1));
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'ProductRepository' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Empieza con un tipo de producto y un repositorio que compile:

```csharp
namespace Generics;

public sealed record Product(int Id, string Name);

public sealed class ProductRepository
{
    public void Add(Product item)
    {
    }

    public Product? Get(int id)
    {
        return null;
    }
}
```

El test compila y falla con `Expected:<Product { Id = 1, Name = Keyboard }>. Actual:<null>`.

## Escribe el código suficiente para que pase

```csharp
public sealed class ProductRepository
{
    private readonly Dictionary<int, Product> _items = new();

    public void Add(Product item)
    {
        _items[item.Id] = item;
    }

    public Product? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

Verde. `ProductRepository` guarda los productos en un diccionario indexado por `Id`.

## Refactor

El repositorio de productos hace una búsqueda y una actualización. Todavía no hay nada que extraer. Ejecuta el test otra vez: sigue en verde. Los siguientes requisitos mostrarán si este algoritmo también sirve para otra entidad.

## Repite con requisitos nuevos

### Un id que no existe

Escribe el test:

```csharp
[TestMethod]
public void GetMissing_ReturnsNull()
{
    var repository = new ProductRepository();

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
    var repository = new ProductRepository();
    repository.Add(new Product(1, "Keyboard"));
    repository.Add(new Product(2, "Mouse"));

    var all = new List<Product>(repository.All);

    Assert.AreEqual(2, all.Count);
}
```

El compilador dice que falta `All`. Agrega una vista de solo lectura:

```csharp
public IReadOnlyCollection<Product> All => _items.Values;
```

Verde. `Dictionary<int, Product>.Values` implementa `IReadOnlyCollection<Product>`, y la propiedad la expone sin dejar que quien llama agregue o quite entradas.

### Un segundo tipo de entidad

Escribe el test:

```csharp
[TestMethod]
public void WorksWithAnotherEntityType()
{
    var repository = new CustomerRepository();
    repository.Add(new Customer(7, "Ada"));

    Assert.AreEqual("Ada", repository.Get(7)!.Name);
}
```

Agrega un cliente y un repositorio con las mismas operaciones:

```csharp
public sealed record Customer(int Id, string Name);

public sealed class CustomerRepository
{
    private readonly Dictionary<int, Customer> _items = new();

    public IReadOnlyCollection<Customer> All => _items.Values;

    public void Add(Customer item)
    {
        _items[item.Id] = item;
    }

    public Customer? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

El test pasa, pero `ProductRepository` y `CustomerRepository` ahora duplican la misma búsqueda y actualización. Los dos tests deben seguir pasando mientras quitamos esa duplicación.

### Compartir una implementación de repositorio

Actualiza los dos tipos de entidad para que implementen el contrato compartido más pequeño:

```csharp
public interface IEntity
{
    int Id { get; }
}

public sealed record Product(int Id, string Name) : IEntity;
public sealed record Customer(int Id, string Name) : IEntity;
```

Ahora los tests eligen el tipo de entidad cuando crean el repositorio:

```csharp
var products = new Repository<Product>();
products.Add(new Product(1, "Keyboard"));
Assert.AreEqual("Keyboard", products.Get(1)!.Name);

var customers = new Repository<Customer>();
customers.Add(new Customer(7, "Ada"));
Assert.AreEqual("Ada", customers.Get(7)!.Name);
```

Reemplaza las dos clases concretas por una implementación genérica:

```csharp
public sealed class Repository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public IReadOnlyCollection<T> All => _items.Values;

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

Todos los tests de comportamiento siguen pasando. El parámetro de tipo quita el código duplicado y mantiene cada repositorio ligado a un tipo de entidad.

### Entender la restricción genérica

`T` es el parámetro de tipo. `new Repository<Product>()` fija `T` como `Product`; `new Repository<Customer>()` lo fija como `Customer`. El compilador comprueba cada tipo construido, aunque ambos comparten una implementación.

La cláusula `where T : class, IEntity` es una restricción. `class` dice que `T` es un tipo por referencia, lo que permite que `Get` devuelva `T?` cuando falta un id. `IEntity` dice que cada `T` tiene una propiedad `Id`, que usa `Add`. Sin esa restricción, el compilador rechazaría `item.Id` porque un `T` cualquiera no tiene miembros conocidos.

### Mantener acotada la restricción

El repositorio es pequeño. La decisión clave es la restricción, que pide `Id` y nada más. Una restricción más amplia, como una clase base con muchos miembros, ataría el repositorio a más de lo necesario y haría más difícil reutilizarlo.

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

Siguiente: [IEnumerable](../enumerable/enumerable.es.md).

