using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Methods.Tests;

[TestClass]
public sealed class ShippingCalculatorTests
{
    [TestMethod]
    public void Cost_StandardShipping_ChargesTheRatePerKg()
    {
        var calculator = new ShippingCalculator();

        Assert.AreEqual(10m, calculator.Cost(2m));
    }

    [TestMethod]
    public void Cost_UsesTheConfiguredRate()
    {
        var calculator = new ShippingCalculator { RatePerKg = 8m };

        Assert.AreEqual(16m, calculator.Cost(2m));
    }

    [TestMethod]
    public void Cost_ExpressShipping_AddsTheSurcharge()
    {
        var calculator = new ShippingCalculator();

        Assert.AreEqual(30m, calculator.Cost(2m, express: true));
    }
}

