using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exceptions.Tests;

[TestClass]
public sealed class AgeTests
{
    [TestMethod]
    public void Parse_Number_ReturnsTheAge()
    {
        Assert.AreEqual(42, Age.Parse("42"));
    }

    [DataTestMethod]
    [DataRow("abc")]
    [DataRow("-1")]
    [DataRow("130")]
    public void Parse_InvalidAge_ThrowsInvalidAge(string text)
    {
        Assert.ThrowsException<InvalidAgeException>(() => Age.Parse(text));
    }
}

