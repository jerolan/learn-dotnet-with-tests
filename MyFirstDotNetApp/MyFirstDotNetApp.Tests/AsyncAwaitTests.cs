using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class AsyncAwaitTests
    {
        [TestMethod]
        public async Task FetchDataAsync_ReturnsExpectedData()
        {
            // Arrange
            var service = new DataService();

            // Act
            var data = await service.FetchDataAsync();

            // Assert
            Assert.AreEqual("Expected Data", data);
        }
    }
}
