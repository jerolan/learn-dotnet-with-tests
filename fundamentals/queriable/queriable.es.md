# IQueryable

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/queriable)

`IQueryable<T>` expone un árbol de expresión, el tipo de los elementos y un proveedor. Una consulta LINQ sobre esta interfaz describe trabajo como datos, y el proveedor decide cómo ejecutarla. Con Entity Framework Core, el proveedor traduce el árbol a SQL.

Vamos a construir un repositorio que devuelve `IQueryable<Order>` y un servicio que compone un filtro. La solución terminada está en `fundamentals/queriable`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Queriable`, un proyecto de MSTest en `tests/Queriable.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

`IQueryable<T>` agrega `Expression`, `ElementType` y `Provider` a la interfaz enumerable. `Queryable.Where` guarda su lambda en un árbol de expresión en vez de compilarla como delegado. Un proveedor puede inspeccionar ese árbol y traducirlo; `AsQueryable()` sobre una lista sigue ejecutándose en memoria con LINQ to Objects.

## Cómo probarlo

Usa una secuencia en memoria con `AsQueryable()` para probar el comportamiento y revisar el árbol de expresión. Enumera solo cuando el test necesite resultados, así también puede comprobar que la fuente queda diferida.

## Escribe primero el test

```csharp
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Queriable.Tests;

[TestClass]
public sealed class QueryTests
{
    private static readonly Order[] Orders =
    {
        new("Ada", 100m),
        new("Grace", 200m),
        new("Linus", 75m)
    };

    [TestMethod]
    public void Expensive_FiltersWhenEnumerated()
    {
        var repository = new OrderRepository(Orders);
        var query = OrderService.Expensive(repository.Query(), 100m);

        var results = new List<Order>(query);

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Ada", results[0].Customer);
        Assert.AreEqual("Grace", results[1].Customer);
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Order' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Queriable;

public sealed record Order(string Customer, decimal Total);

public sealed class OrderRepository
{
    public IQueryable<Order> Query()
    {
        return Enumerable.Empty<Order>().AsQueryable();
    }
}

public static class OrderService
{
    public static IQueryable<Order> Expensive(IQueryable<Order> orders, decimal minimum)
    {
        return orders;
    }
}
```

El test compila y falla con `Expected:<2>. Actual:<0>`. La consulta está vacía porque el repositorio todavía no tiene una fuente.

## Escribe el código suficiente para que pase

```csharp
public sealed class OrderRepository
{
    private readonly IEnumerable<Order> _orders;

    public OrderRepository(IEnumerable<Order> orders)
    {
        _orders = orders;
    }

    public IQueryable<Order> Query()
    {
        return _orders.AsQueryable();
    }
}

public static class OrderService
{
    public static IQueryable<Order> Expensive(IQueryable<Order> orders, decimal minimum)
    {
        return orders.Where(order => order.Total >= minimum);
    }
}
```

Verde. `AsQueryable()` envuelve la secuencia en un `EnumerableQuery<Order>`, el proveedor de LINQ to Objects. El servicio llama a `Where` sobre el queryable; el resultado es otro queryable.

## Refactor

El servicio compone un filtro y devuelve el queryable para que lo ejecute su proveedor. Ejecuta el test otra vez; no hay otra capa que quitar.

## Repite con requisitos nuevos

### La consulta es un dato

Escribe el test:

```csharp
[TestMethod]
public void Expensive_IsAnExpressionTree()
{
    var repository = new OrderRepository(Orders);
    var query = OrderService.Expensive(repository.Query(), 100m);

    var call = query.Expression as MethodCallExpression;

    Assert.IsNotNull(call);
    Assert.AreEqual("Where", call.Method.Name);
}
```

Pasa. `query.Expression` es un `MethodCallExpression` de `Where`. El predicado está guardado como dato dentro de ese árbol; todavía no corrió nada sobre los pedidos.

Esa es la diferencia que un proveedor puede usar. EF Core recorre el árbol, escribe una cláusula `WHERE` y le pide las filas a la base de datos. El código del servicio se ve igual si el proveedor es una lista o SQL Server.

### La fuente no se enumera antes

Escribe el test:

```csharp
[TestMethod]
public void Query_DoesNotEnumerateTheSource()
{
    var log = new List<string>();
    var repository = new OrderRepository(Source(log));

    var query = repository.Query();

    Assert.AreEqual(0, log.Count);

    var results = new List<Order>(query);

    Assert.AreEqual(3, log.Count);
    Assert.AreEqual(3, results.Count);
}

private static IEnumerable<Order> Source(List<string> log)
{
    foreach (var order in Orders)
    {
        log.Add(order.Customer);
        yield return order;
    }
}
```

Falla si el repositorio materializa la fuente en el constructor:

```csharp
public OrderRepository(IEnumerable<Order> orders)
{
    _orders = orders.ToList();
}
```

Con esa versión, `log` ya tiene tres entradas cuando `Query()` retorna. Guarda la secuencia en vez de copiarla:

```csharp
public OrderRepository(IEnumerable<Order> orders)
{
    _orders = orders;
}
```

Verde. `Query()` devuelve un queryable sin tocar la fuente; la fuente se enumera cuando se enumera la consulta. Es la misma ejecución diferida que `IEnumerable`, con el árbol de expresión guardado para el proveedor.

### Elegir el límite de la consulta

El código es pequeño; el borde es la parte interesante.

Devolver `IQueryable<T>` desde un repositorio le entrega a quien llama el proveedor y la expresión. Quien llama puede componer operadores arbitrarios, y esos operadores corren donde corra el proveedor. Con EF Core, una consulta compuesta lejos del repositorio puede producir SQL que no esperabas, con joins extra o columnas que la pantalla no necesita.

Usa `IQueryable` cuando la composibilidad sea el punto: un repositorio que soporta un objeto de consulta, o un servicio que arma un filtro a partir de la entrada del usuario. Devuelve una lista materializada o un DTO cuando el borde está completo. `ToList()` o `AsEnumerable()` pasan el resto del pipeline a evaluación en el cliente; hazlo a propósito.

No mockees `IQueryable`. La versión vieja de este capítulo armaba un mock con `Provider`, `Expression` y `GetEnumerator` configurados a mano; el test probaba que el mock podía devolver una lista, no que un proveedor de consultas funcionara. Prueba la consulta contra un proveedor real: LINQ to Objects para la forma, EF Core con SQLite o un contenedor de test para la traducción.

## Para cerrar

Lo que cubrimos:

- `IQueryable<T>` como `IEnumerable<T>` más `Expression` y `Provider`.
- `AsQueryable()` y el proveedor de LINQ to Objects.
- Árboles de expresión en vez de delegados para los operadores queryable.
- Ejecución diferida en el borde del repositorio.
- Por qué un proveedor como EF Core puede traducir la misma consulta a SQL.
- El costo de filtrar `IQueryable` entre capas, y por qué no mockearlo.

El capítulo se apoya en el [capítulo LINQ](../linq/linq.es.md): los operadores son los mismos, pero el tipo destino decide si compilan a delegados o a un árbol de expresión. EF Core hará concreto el proveedor en la Parte 3.

Siguiente: [La palabra ref](../ref/ref.es.md).

