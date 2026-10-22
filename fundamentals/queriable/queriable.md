# IQueryable

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/queriable)

`IQueryable<T>` exposes an expression tree, an element type and a provider. A LINQ query over it describes work as data, and the provider decides how to execute it. With Entity Framework Core, the provider translates the tree to SQL.

We will build a repository that returns `IQueryable<Order>` and a service that composes a filter. The finished solution is in `fundamentals/queriable`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Queriable`, an MSTest project in `tests/Queriable.Tests`, a solution and a project reference.

## How it works

`IQueryable<T>` adds `Expression`, `ElementType` and `Provider` to the enumerable interface. `Queryable.Where` stores its lambda in an expression tree instead of compiling it to a delegate. A provider can inspect that tree and translate it; `AsQueryable()` over a list still runs in memory through LINQ to Objects.

## How to test

Use an in-memory sequence with `AsQueryable()` to test query behavior and inspect its expression tree. Enumerate only when the test needs results, so it can also verify that the source stays deferred.

## Write the test first

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

## Try to run the test

```text
error CS0246: The type or namespace name 'Order' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

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

The test compiles and fails with `Expected:<2>. Actual:<0>`. The query is empty because the repository has no source yet.

## Write enough code to make it pass

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

Green. `AsQueryable()` wraps the sequence in an `EnumerableQuery<Order>`, the provider for LINQ to Objects. The service calls `Where` on the queryable; the result is another queryable.

## Refactor

The service composes one filter and returns the queryable so its provider can execute it. Run the test again; there is no extra layer to remove.

## Repeat for new requirements

### The query is data

Write the test:

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

It passes. `query.Expression` is a `MethodCallExpression` for `Where`. The predicate is stored as data inside that tree; nothing has run against the orders yet.

This is the difference a provider can use. EF Core walks the tree, writes a `WHERE` clause, and asks the database for the rows. The service code looks the same whether the provider is a list or SQL Server.

### The source is not enumerated early

Write the test:

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

It fails if the repository materializes the source in its constructor:

```csharp
public OrderRepository(IEnumerable<Order> orders)
{
    _orders = orders.ToList();
}
```

With that version, `log` already has three entries when `Query()` returns. Store the sequence instead of copying it:

```csharp
public OrderRepository(IEnumerable<Order> orders)
{
    _orders = orders;
}
```

Green. `Query()` returns a queryable without touching the source; the source is enumerated when the query is enumerated. That is the same deferred execution as `IEnumerable`, with the expression tree kept for the provider.

### Choosing the query boundary

The code is small; the boundary is the interesting part.

Returning `IQueryable<T>` from a repository hands the caller the provider and the expression. The caller can compose arbitrary operators, and those operators run wherever the provider runs them. With EF Core, a query composed far from the repository can produce SQL you did not expect, including extra joins or a query that loads more columns than the screen needs.

Use `IQueryable` when composability is the point: a repository that supports a query object, or a service that builds a filter from user input. Return a materialized list or a DTO when the boundary is complete. `ToList()` or `AsEnumerable()` switches the rest of the pipeline to client evaluation; do it deliberately.

Do not mock `IQueryable`. The old version of this chapter built a mock with `Provider`, `Expression` and `GetEnumerator` set up by hand; the test proved that the mock could return a list, not that a query provider worked. Test the query against a real provider: LINQ to Objects for the shape, EF Core with SQLite or a test container for the translation.

## Wrapping up

What we have covered:

- `IQueryable<T>` as `IEnumerable<T>` plus `Expression` and `Provider`.
- `AsQueryable()` and the LINQ to Objects provider.
- Expression trees instead of delegates for queryable operators.
- Deferred execution at the repository boundary.
- Why a provider such as EF Core can translate the same query to SQL.
- The cost of leaking `IQueryable` across layers, and why not to mock it.

The chapter builds on the [LINQ chapter](../linq/linq.md): the operators are the same, but the target type decides whether they compile to delegates or to an expression tree. EF Core will make the provider concrete in Part 3.

Next: [The ref keyword](../ref/ref.md).

