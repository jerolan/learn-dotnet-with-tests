using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HelloWorld.Tests;

[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void SayHello_ReturnsHelloWorld()
    {
        var greeter = new Greeter();
        Assert.AreEqual("Hello, World!", greeter.SayHello());
    }
}
