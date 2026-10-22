using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WithKeyword.Tests;

[TestClass]
public sealed class RecordCopyTests
{
    [TestMethod]
    public void With_CreatesAModifiedCopy()
    {
        var original = new Person("Ada", 36);

        var older = original with { Age = 37 };

        Assert.AreEqual(36, original.Age);
        Assert.AreEqual(37, older.Age);
        Assert.AreEqual("Ada", older.Name);
    }

    [TestMethod]
    public void With_AnEmptyInitializer_CreatesAnEqualCopy()
    {
        var original = new Person("Ada", 36);

        var copy = original with { };

        Assert.AreEqual(original, copy);
    }

    [TestMethod]
    public void With_NestedRecord_UpdatesOnlyTheInnerValue()
    {
        var customer = new Customer("Ada", new Address("London"));

        var moved = customer with { Home = customer.Home with { City = "Paris" } };

        Assert.AreEqual("London", customer.Home.City);
        Assert.AreEqual("Paris", moved.Home.City);
        Assert.AreEqual("Ada", moved.Name);
    }
}

