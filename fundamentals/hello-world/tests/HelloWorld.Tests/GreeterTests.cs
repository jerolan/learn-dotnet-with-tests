using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HelloWorld.Tests;

[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void SayHelloToPeople()
    {
        AssertCorrectMessage("Hello, Chris", new Greeter().SayHello("Chris"));
    }

    [TestMethod]
    public void SayHelloToWorldWhenEmptyStringIsSupplied()
    {
        AssertCorrectMessage("Hello, World", new Greeter().SayHello(string.Empty));
    }

    private static void AssertCorrectMessage(string want, string got)
    {
        Assert.AreEqual(want, got);
    }
}
