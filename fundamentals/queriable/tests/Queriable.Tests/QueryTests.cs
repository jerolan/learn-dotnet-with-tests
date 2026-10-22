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

    [TestMethod]
    public void Expensive_IsAnExpressionTree()
    {
        var repository = new OrderRepository(Orders);
        var query = OrderService.Expensive(repository.Query(), 100m);

        var call = query.Expression as MethodCallExpression;

        Assert.IsNotNull(call);
        Assert.AreEqual("Where", call.Method.Name);
    }

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
}

