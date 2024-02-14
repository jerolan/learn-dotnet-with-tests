using MyFirstDotNetApp;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class HelloWorldTests
    {
        [TestMethod]
        public void SayHello_ReturnsHelloWorld()
        {
            // Arrange
            var app = new HelloWorld();

            // Act
            string result = app.SayHello();

            // Assert
            Assert.AreEqual("Hello, World!", result);
        }
    }
}
