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

    [TestMethod]
    public void TopCustomers_OrdersByTotalAndName()
    {
        var top = OrderQueries.TopCustomers(Orders, 3);

        Assert.AreEqual(3, top.Count);
        Assert.AreEqual("Grace", top[0]);
        Assert.AreEqual("Ada", top[1]);
        Assert.AreEqual("Margaret", top[2]);
    }

    [TestMethod]
    public void HasOrdersOver_ChecksTheThreshold()
    {
        Assert.IsTrue(OrderQueries.HasOrdersOver(Orders, 150m));
        Assert.IsFalse(OrderQueries.HasOrdersOver(Orders, 500m));
    }
}

