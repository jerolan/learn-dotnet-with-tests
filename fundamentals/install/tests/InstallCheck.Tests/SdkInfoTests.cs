using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InstallCheck.Tests;

[TestClass]
public sealed class SdkInfoTests
{
    [TestMethod]
    public void GetRuntimeVersion_ReturnsNonEmpty()
    {
        var version = InstallCheck.SdkInfo.GetRuntimeVersion();
        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
    }
}
