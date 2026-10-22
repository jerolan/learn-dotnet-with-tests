using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Mocking.Tests;

[TestClass]
public sealed class UserServiceTests
{
    [TestMethod]
    public void GetName_ReturnsTheNameFromTheRepository()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.Find(1)).Returns(new User("Ada"));
        var service = new UserService(repository.Object);

        var name = service.GetName(1);

        Assert.AreEqual("Ada", name);
        repository.Verify(r => r.Find(1), Times.Once);
    }

    [TestMethod]
    public void GetName_UnknownUser_ReturnsUnknown()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.Find(99)).Returns((User?)null);
        var service = new UserService(repository.Object);

        var name = service.GetName(99);

        Assert.AreEqual("Unknown", name);
    }
}

