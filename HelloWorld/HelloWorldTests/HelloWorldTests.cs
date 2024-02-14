using HelloWorld;

namespace HelloWorldTests
{
    [TestClass]
    public class GreeterTests
    {
        [TestMethod]
        public void SayHello_ReturnsHelloWorld()
        {
            // Arrange
            var greeter = new Greeter();

            // Act
            var result = greeter.SayHello();

            // Assert
            Assert.AreEqual("Hello, World!", result);
        }
    }
}
