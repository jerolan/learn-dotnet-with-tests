using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Records.Tests;

[TestClass]
public sealed class CounterTests
{
    [TestMethod]
    public void Assignment_CopiesTheStruct()
    {
        var original = new Counter { Value = 1 };
        var copy = original;

        copy.Increment();

        Assert.AreEqual(1, original.Value);
        Assert.AreEqual(2, copy.Value);
    }
}

[TestClass]
public sealed class PersonTests
{
    [TestMethod]
    public void TwoPeopleWithTheSameValues_AreEqual()
    {
        Assert.AreEqual(new Person("Ada", 36), new Person("Ada", 36));
    }

    [TestMethod]
    public void TwoPeopleWithDifferentValues_AreNotEqual()
    {
        Assert.AreNotEqual(new Person("Ada", 36), new Person("Grace", 36));
    }

    [TestMethod]
    public void IsAdult_ChecksTheAge()
    {
        Assert.IsTrue(new Person("Ada", 36).IsAdult);
        Assert.IsFalse(new Person("Tim", 10).IsAdult);
    }
}

