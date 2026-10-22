using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Types.Tests;

[TestClass]
public sealed class TypeExamplesTests
{
    [DataTestMethod]
    [DataRow(2, 2, 4)]
    [DataRow(1, 5, 6)]
    [DataRow(-3, 3, 0)]
    public void Add_ReturnsSum(int x, int y, int want)
    {
        Assert.AreEqual(want, TypeExamples.Add(x, y));
    }

    [TestMethod]
    public void DivideIntegers_TruncatesTheFraction()
    {
        Assert.AreEqual(2, TypeExamples.Divide(10, 4));
    }

    [TestMethod]
    public void DivideDoubles_KeepsTheFraction()
    {
        Assert.AreEqual(2.5, TypeExamples.Divide(10.0, 4.0));
    }

    [TestMethod]
    public void IsPositive_ChecksTheSign()
    {
        Assert.IsTrue(TypeExamples.IsPositive(5));
        Assert.IsFalse(TypeExamples.IsPositive(-1));
        Assert.IsFalse(TypeExamples.IsPositive(0));
    }
}

