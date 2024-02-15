## Dependency Injection

Dependency Injection (DI) is a design pattern that promotes loose coupling between components, making systems more flexible, scalable, and easier to test. In DotNet, especially with .NET Core and ASP.NET Core, DI is a first-class citizen, integrated directly into the framework. This chapter will explore the fundamentals of DI, its benefits, and how to implement it effectively in DotNet applications. We'll use Test-Driven Development (TDD) with MSTest to illustrate how DI facilitates better unit testing and overall application design.

### Understanding Dependency Injection

DI allows a component's dependencies to be injected at runtime rather than at compile time, enabling more modular and testable code. There are three primary methods of dependency injection:

- Constructor Injection
- Property Injection
- Method Injection

Constructor injection is the most common and recommended approach in DotNet applications due to its clarity and the immutability it offers for dependencies.

### Setting Up for Dependency Injection

To effectively use DI in DotNet, you'll typically work with the `IServiceCollection` interface available in the `Microsoft.Extensions.DependencyInjection` namespace, which allows registering and configuring services.

1. **Add Dependency Injection Package**: For non-ASP.NET Core applications, ensure you have the `Microsoft.Extensions.DependencyInjection` NuGet package installed.

   ```
   dotnet add ./MyFirstDotNetApp/MyFirstDotNetApp.csproj package Microsoft.Extensions.DependencyInjection
   ```

2. **Create a Test Class for DI**: Navigate to your test project directory and create a test class named `DependencyInjectionTests.cs`.

### Writing Tests Demonstrating DI

#### Testing with Constructor Injection

Let's demonstrate DI with a simple service and its interface that will be injected into a consumer class.

Write a test that uses DI to inject the `GreetingService` into the `Consumer` class:

```csharp
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
```

### Implement the necessary code to make the test pass

Define the service interface and its implementation:

```csharp
public interface IGreetingService
{
    string Greet(string name);
}

public class GreetingService : IGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}
```

Next, create a consumer class that depends on `IGreetingService`:

```csharp
public class Consumer
{
    private readonly IGreetingService _greetingService;

    public Consumer(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    public string SayHello(string name) => _greetingService.Greet(name);
}
```

### Best Practices for Using Dependency Injection

- **Prefer Constructor Injection**: It makes dependencies clear and ensures they're set before any methods are called.
- **Register Services with Appropriate Lifetimes**: Understand the differences between transient, scoped, and singleton lifetimes.
- **Avoid Service Locator Pattern**: Directly requesting services from the DI container (service locator pattern) can lead to less clear and maintainable code compared to constructor injection.
- **Use Interfaces for Dependencies**: This promotes loose coupling and makes unit testing easier.

### Conclusion

Dependency Injection is a powerful pattern for building maintainable, testable, and loosely coupled applications. By integrating DI into your DotNet projects, you can improve the design and quality of your code. The .NET Core and ASP.NET Core frameworks provide built-in support for DI, making it easier than ever to apply this pattern. Through the use of TDD, you can ensure that your components are correctly designed to take advantage of DI, leading to more robust and flexible applications.
