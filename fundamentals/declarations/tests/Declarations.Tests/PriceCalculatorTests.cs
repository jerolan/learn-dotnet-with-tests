using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Declarations.Tests;

[TestClass]
public sealed class PriceCalculatorTests
{
    [TestMethod]
    public void TotalWithVat_AddsTheDefaultRate()
    {
        var calculator = new PriceCalculator();

        Assert.AreEqual(116m, calculator.TotalWithVat(100m));
    }

    [TestMethod]
    public void TotalWithVat_UsesTheRatePassedToTheConstructor()
    {
        var calculator = new PriceCalculator(0.08m);

        Assert.AreEqual(108m, calculator.TotalWithVat(100m));
    }
}

