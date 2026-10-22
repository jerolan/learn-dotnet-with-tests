using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ref.Tests;

[TestClass]
public sealed class ValuesTests
{
    [TestMethod]
    public void Swap_ExchangesTheValues()
    {
        var left = 1;
        var right = 2;

        Values.Swap(ref left, ref right);

        Assert.AreEqual(2, left);
        Assert.AreEqual(1, right);
    }

    [TestMethod]
    public void TryDivide_ByZero_ReturnsFalseAndZero()
    {
        var result = Values.TryDivide(10, 0, out var quotient);

        Assert.IsFalse(result);
        Assert.AreEqual(0, quotient);
    }

    [TestMethod]
    public void TryDivide_ByDivisor_ReturnsTheQuotient()
    {
        var result = Values.TryDivide(10, 2, out var quotient);

        Assert.IsTrue(result);
        Assert.AreEqual(5, quotient);
    }

    [TestMethod]
    public void At_ReturnsARefToTheSlot()
    {
        var values = new[] { 1, 2, 3 };

        ref var slot = ref Values.At(values, 1);
        slot = 20;

        Assert.AreEqual(20, values[1]);
    }

    [TestMethod]
    public void ManhattanDistance_AddsTheCoordinateDifferences()
    {
        var origin = new Point(0, 0);

        Assert.AreEqual(7, origin.ManhattanDistance(new Point(3, 4)));
    }
}

