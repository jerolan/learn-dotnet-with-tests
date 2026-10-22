using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InstallCheck.Tests;

[TestClass]
public sealed class SdkInfoTests
{
    [TestMethod]
    public void GetDotnetVersion_ReturnsNonEmpty()
    {
        var version = InstallCheck.SdkInfo.GetDotnetVersion();
        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
    }
}
