# LINQ

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/linq)

LINQ es un conjunto de operadores de consulta para secuencias: filtrar, proyectar, agrupar, ordenar, agregar. Los operadores se componen, y la mayoría no hace nada hasta que algo enumera el resultado. Eso convierte una consulta en una descripción del trabajo, no en el trabajo en sí.

Vamos a consultar una lista de pedidos por cliente y total. La solución terminada está en `fundamentals/linq`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Linq`, un proyecto de MSTest en `tests/Linq.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Los operadores LINQ como `Where`, `Select` y `GroupBy` describen trabajo sobre un `IEnumerable<T>`. Muchos devuelven una secuencia diferida; operadores como `Sum`, `Any` y `ToList` la enumeran para producir una respuesta o una instantánea.

## Cómo probarlo

Pasa un arreglo de pedidos en memoria a cada consulta y comprueba el resultado. Los tests pueden revisar totales, orden y límites sin una base de datos ni un sistema de archivos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Linq.Tests;

[TestClass]
public sealed class OrderQueriesTests
{
    private static readonly Order[] Orders =
    {
        new("Ada", 100m),
        new("Ada", 50m),
        new("Grace", 200m),
        new("Linus", 75m),
        new("Margaret", 150m)
    };

    [TestMethod]
    public void TotalFor_SumsOnlyThatCustomer()
    {
        Assert.AreEqual(150m, OrderQueries.TotalFor(Orders, "Ada"));
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'OrderQueries' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Linq;

public sealed record Order(string Customer, decimal Total);

public static class OrderQueries
{
    public static decimal TotalFor(IEnumerable<Order> orders, string customer)
    {
        return 0;
    }
}
```

El código compila y el test falla con `Expected:<150>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public static decimal TotalFor(IEnumerable<Order> orders, string customer)
{
    return orders
        .Where(order => order.Customer == customer)
        .Sum(order => order.Total);
}
```

Verde. La cadena se lee de arriba a abajo:

- `Where` deja solo los pedidos cuyo cliente coincide. La lambda `order => order.Customer == customer` es el predicado.
- `Sum` suma el `Total` de los pedidos que quedan. La lambda selecciona el valor a sumar.

Las dos son métodos de extensión sobre `IEnumerable<Order>`. Cada uno devuelve una secuencia nueva o un valor único, y por eso las llamadas se encadenan.

## Refactor

`TotalFor` ya es una consulta corta sin lógica duplicada que extraer. Ejecuta el test otra vez; sigue en verde. Deja los operadores visibles para que la consulta se lea en el orden en que corre.

## Repite con requisitos nuevos

### Ordenar los clientes

Escribe el test:

```csharp
[TestMethod]
public void TopCustomers_OrdersByTotalAndName()
{
    var top = OrderQueries.TopCustomers(Orders, 3);

    Assert.AreEqual(3, top.Count);
    Assert.AreEqual("Grace", top[0]);
    Assert.AreEqual("Ada", top[1]);
    Assert.AreEqual("Margaret", top[2]);
}
```

El compilador dice que falta `TopCustomers`. Agrégala:

```csharp
public static IReadOnlyList<string> TopCustomers(IEnumerable<Order> orders, int count)
{
    return orders
        .GroupBy(order => order.Customer)
        .OrderByDescending(group => group.Sum(order => order.Total))
        .ThenBy(group => group.Key)
        .Take(count)
        .Select(group => group.Key)
        .ToList();
}
```

Verde. Lee la cadena en orden:

- `GroupBy(order => order.Customer)` produce un grupo por cliente. Cada grupo es un `IGrouping<string, Order>` con una `Key` y los pedidos adentro.
- `OrderByDescending(group => group.Sum(order => order.Total))` ordena los grupos por su total, de mayor a menor.
- `ThenBy(group => group.Key)` rompe empates por nombre de cliente, así el resultado es determinista. Ada y Margaret tienen `150`; Ada va primero.
- `Take(count)` conserva los primeros `count` grupos.
- `Select(group => group.Key)` proyecta cada grupo a su nombre de cliente.
- `ToList()` materializa el resultado, así el método devuelve una lista estable en vez de una consulta diferida.

### Una pregunta de sí o no

Escribe el test:

```csharp
[TestMethod]
public void HasOrdersOver_ChecksTheThreshold()
{
    Assert.IsTrue(OrderQueries.HasOrdersOver(Orders, 150m));
    Assert.IsFalse(OrderQueries.HasOrdersOver(Orders, 500m));
}
```

Agrega el método:

```csharp
public static bool HasOrdersOver(IEnumerable<Order> orders, decimal amount)
{
    return orders.Any(order => order.Total > amount);
}
```

Verde. `Any` se detiene en la primera coincidencia; no recorre el resto de la secuencia.

### Ejecución diferida y sintaxis de consulta

El código es un conjunto de consultas pequeñas. El comportamiento importante es la ejecución diferida.

`Where` y `Select` devuelven un `IEnumerable<T>` sin hacer el trabajo. `Sum`, `Any`, `Count` y `ToList` ejecutan la consulta porque necesitan una respuesta. Asignar una consulta a una variable no la ejecuta:

```csharp
var expensive = Orders.Where(order => order.Total > 100m);

// todavía no se filtró nada

foreach (var order in expensive)
{
    // ahora corre el predicado
}
```

Una consulta diferida corre de nuevo cada vez que se enumera. Si la fuente cambia entre pasadas, las dos pasadas pueden ver datos distintos. Materializa con `ToList` cuando necesites una foto o cuando vayas a enumerar más de una vez.

LINQ también tiene sintaxis de consulta:

```csharp
var totals = from order in orders
             where order.Customer == customer
             select order.Total;

return totals.Sum();
```

Compila a las mismas llamadas `Where` y `Select`. Este libro usa sintaxis de método porque se compone bien con los operadores que no tienen palabra clave en la sintaxis de consulta, como `ThenBy`.

## Para cerrar

Lo que cubrimos:

- `Where` y `Sum` con lambdas.
- `GroupBy` y `IGrouping.Key`.
- `OrderByDescending` y `ThenBy` para un ranking determinista.
- `Take`, `Select` y `ToList`.
- `Any` como comprobación que corta en la primera coincidencia.
- Ejecución diferida, enumeración repetida y materialización de una foto.
- Sintaxis de método frente a sintaxis de consulta.

El capítulo sigue las operaciones de secuencia del [capítulo Generics de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/generics), donde una función genérica pequeña reemplaza un bucle. LINQ generaliza esa idea a filtrado, proyección, agrupación y orden.

Siguiente: [IQueryable](../queriable/queriable.es.md).

