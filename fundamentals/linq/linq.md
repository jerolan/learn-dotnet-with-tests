# LINQ

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/linq)

LINQ is a set of query operators for sequences: filter, project, group, order, aggregate. The operators compose, and most of them do nothing until something enumerates the result. That makes a query a description of work rather than the work itself.

We will query a list of orders by customer and total. The finished solution is in `fundamentals/linq`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Linq`, an MSTest project in `tests/Linq.Tests`, a solution and a project reference.

## How it works

LINQ operators such as `Where`, `Select` and `GroupBy` describe work over an `IEnumerable<T>`. Many return a deferred sequence; operators such as `Sum`, `Any` and `ToList` enumerate it to produce an answer or a snapshot.

## How to test

Pass an in-memory array of orders to each query and assert the result. The tests can check totals, ordering and thresholds without a database or file system.

## Write the test first

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

## Try to run the test

```text
error CS0246: The type or namespace name 'OrderQueries' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

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

The code compiles and the test fails with `Expected:<150>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public static decimal TotalFor(IEnumerable<Order> orders, string customer)
{
    return orders
        .Where(order => order.Customer == customer)
        .Sum(order => order.Total);
}
```

Green. The chain reads top to bottom:

- `Where` keeps only the orders whose customer matches. The lambda `order => order.Customer == customer` is the predicate.
- `Sum` adds the `Total` of the remaining orders. The lambda selects the value to add.

Both are extension methods on `IEnumerable<Order>`. Each one returns a new sequence or a single value, which is why the calls chain.

## Refactor

`TotalFor` is already a short query pipeline with no duplicated logic to extract. Run the test again; it remains green. Keep the operators visible so the query reads in the order it runs.

## Repeat for new requirements

### Rank the customers

Write the test:

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

The compiler says `TopCustomers` is missing. Add it:

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

Green. Read the chain in order:

- `GroupBy(order => order.Customer)` produces one group per customer. Each group is an `IGrouping<string, Order>` with a `Key` and the orders inside.
- `OrderByDescending(group => group.Sum(order => order.Total))` sorts the groups by their total, highest first.
- `ThenBy(group => group.Key)` breaks ties by customer name, so the result is deterministic. Ada and Margaret both have `150`; Ada comes first.
- `Take(count)` keeps the first `count` groups.
- `Select(group => group.Key)` projects each group to its customer name.
- `ToList()` materializes the result, so the method returns a stable list instead of a deferred query.

### Ask a yes-or-no question

Write the test:

```csharp
[TestMethod]
public void HasOrdersOver_ChecksTheThreshold()
{
    Assert.IsTrue(OrderQueries.HasOrdersOver(Orders, 150m));
    Assert.IsFalse(OrderQueries.HasOrdersOver(Orders, 500m));
}
```

Add the method:

```csharp
public static bool HasOrdersOver(IEnumerable<Order> orders, decimal amount)
{
    return orders.Any(order => order.Total > amount);
}
```

Green. `Any` stops at the first match; it does not walk the rest of the sequence.

### Deferred execution and query syntax

The code is a set of small queries. The important behavior is deferred execution.

`Where` and `Select` return an `IEnumerable<T>` without doing the work. `Sum`, `Any`, `Count` and `ToList` execute the query because they need an answer. Assigning a query to a variable does not run it:

```csharp
var expensive = Orders.Where(order => order.Total > 100m);

// nothing has been filtered yet

foreach (var order in expensive)
{
    // now the predicate runs
}
```

A deferred query runs again every time it is enumerated. If the source changes between passes, the two passes can see different data. Materialize with `ToList` when you need a snapshot or when you will enumerate more than once.

LINQ also has a query syntax:

```csharp
var totals = from order in orders
             where order.Customer == customer
             select order.Total;

return totals.Sum();
```

It compiles to the same `Where` and `Select` calls. This book uses method syntax because it composes well with the operators that have no query-syntax keyword, such as `ThenBy`.

## Wrapping up

What we have covered:

- `Where` and `Sum` with lambdas.
- `GroupBy` and `IGrouping.Key`.
- `OrderByDescending` and `ThenBy` for a deterministic ranking.
- `Take`, `Select` and `ToList`.
- `Any` as a short-circuiting check.
- Deferred execution, repeated enumeration and materializing a snapshot.
- Method syntax versus query syntax.

The chapter follows the sequence operations of the [Generics chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/generics), where a small generic function replaces a loop. LINQ generalizes that idea across filtering, projection, grouping and ordering.

Next: [IQueryable](../queriable/queriable.md).

