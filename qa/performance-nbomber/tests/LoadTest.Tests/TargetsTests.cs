using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LoadTest.Tests;

[TestClass]
public sealed class TargetsTests
{
    [TestMethod]
    public void PingUrl_CombinesTheBaseAndThePath()
    {
        Assert.AreEqual(
            "http://localhost:5099/ping",
            Targets.PingUrl("http://localhost:5099"));
    }
}

