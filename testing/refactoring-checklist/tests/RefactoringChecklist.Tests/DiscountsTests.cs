using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefactoringChecklist.Tests;

[TestClass]
public sealed class DiscountsTests
{
    [TestMethod]
    public void Apply_WithPercent_SubtractsItFromThePrice()
    {
        Assert.AreEqual(90m, Discounts.Apply(100m, 10, member: false));
    }

    [TestMethod]
    public void Apply_WithMember_AddsTheExtraDiscount()
    {
        Assert.AreEqual(85.5m, Discounts.Apply(100m, 10, member: true));
    }

    [TestMethod]
    public void Apply_WithInvalidPercent_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => Discounts.Apply(100m, 101, member: false));
    }
}

