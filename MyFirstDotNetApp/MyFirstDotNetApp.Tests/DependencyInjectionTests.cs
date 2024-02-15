using Microsoft.Extensions.DependencyInjection;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class DependencyInjectionTests
    {
        [TestMethod]
        public void Consumer_UsesInjectedGreetingService_ReturnsExpectedGreeting()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<IGreetingService, GreetingService>();
            services.AddTransient<Consumer>();
            var serviceProvider = services.BuildServiceProvider();
            var consumer = serviceProvider.GetService<Consumer>();

            // Act
            var result = consumer.SayHello("DotNet");

            // Assert
            Assert.AreEqual("Hello, DotNet!", result);
        }
    }
}
