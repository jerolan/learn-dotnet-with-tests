using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Decimal.Tests;
[TestClass]
public sealed class MoneyTests
{
    [TestMethod]
    public void AddTwoAmounts()
    {
        var total = new Money(5.5m).Add(new Money(2.5m));
        Assert.AreEqual(new Money(8.0m), total);
    }
    [TestMethod]
    public void MultiplyPriceByQuantity()
    {
        var total = new Money(19.95m).Multiply(3);
        Assert.AreEqual(new Money(59.85m), total);
    }
    [TestMethod]
    public void DecimalTenthsAreExact()
    {
        Assert.AreEqual(0.3m, 0.1m + 0.2m);
    }
    [TestMethod]
    public void DoubleTenthsAreNotExact()
    {
        Assert.AreNotEqual(0.3, 0.1 + 0.2);
    }
    [TestMethod]
    public void RoundingAwayFromZero()
    {
        var rounded = new Money(10.545m).Rounded(2);
        Assert.AreEqual(new Money(10.55m), rounded);
    }
}
