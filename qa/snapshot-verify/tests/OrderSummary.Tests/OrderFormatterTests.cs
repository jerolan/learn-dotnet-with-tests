using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;

namespace OrderSummary.Tests;

[TestClass]
public sealed class OrderFormatterTests : VerifyBase
{
    [TestMethod]
    public Task FormatsTheOrder()
    {
        var order = new[]
        {
            new OrderLine("Keyboard", 2, 49.95m),
            new OrderLine("Mouse", 1, 19.95m)
        };

        return Verify(OrderFormatter.Format(order));
    }
}

