## Hello World: Your First DotNet Test

Welcome to the exciting journey of learning DotNet with Test-Driven Development (TDD). In this chapter, we'll start with the simplest possible scenario - writing a "Hello World" program. However, true to the TDD approach, we'll begin by writing a test for our program before we actually implement the functionality. This will introduce you to the rhythm of TDD: Red, Green, Refactor.

### Understanding the Test Structure with MSTest

MSTest is a testing framework that allows us to write and execute tests in the DotNet environment. A basic test in MSTest includes three key parts:

- **Test Class**: A container for your test methods. This class must be decorated with the `[TestClass]` attribute.
- **Test Method**: Represents a single test. It must be decorated with the `[TestMethod]` attribute.
- **Assertions**: Statements that check if the code behaves as expected. An assertion failing means your test fails.

### Setting Up the "Hello World" Test

Let's create a test that expects our application to return "Hello, World!".

1. **Navigate to Your Test Project**: Make sure you're in the directory of your `MyFirstDotNetApp.Tests` project.

2. **Writing the Test**: Rename the `UnitTest1.cs` file to `HelloWorldTests.cs` and replace its content with the following code:

```csharp
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
```

In this test, we're creating an instance of a class named `HelloWorld` that we expect to have a method `SayHello` returning the string "Hello, World!".

### Implementing the "Hello World" Functionality

If you try to run the test now, it will fail because we haven't implemented the `HelloWorld` class and its `SayHello` method yet. Let's do that next.

We need to implement the `HelloWorld` class and its `SayHello` method in our main project (`MyFirstDotNetApp`).

1. **Create the HelloWorld Class**: Navigate to your main project directory and add a new file named `HelloWorld.cs`. Then, define the `HelloWorld` class with the `SayHello` method:

```csharp
namespace MyFirstDotNetApp
{
    public class HelloWorld
    {
        public string SayHello()
        {
            return "Hello, World!";
        }
    }
}
```

This class simply contains one method that returns the string "Hello, World!".

### Running Your First Test

With the test and the method implemented, it's time to run the test and see the results.

1. **Run the Test**: Open your terminal or command prompt, navigate to the `MyFirstDotNetApp.Tests` directory, and execute the command:
   ```
   dotnet test
   ```
2. **Observe the Outcome**: If everything is set up correctly, your test should pass, indicating that your `SayHello` method behaves as expected.

   ```
   Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 8 ms - /Users/jeromeolverasanchez/workspace/learn-dotnet-with-tests/MyFirstDotNetApp/MyFirstDotNetApp.Tests/bin/Debug/net6.0/MyFirstDotNetApp.Tests.dll (net6.0)
   ```

### Conclusion

Congratulations! You've just written your first test in DotNet using MSTest and followed it up with a simple implementation to make the test pass. This is the essence of TDD - starting with a failing test (Red), implementing the minimal code necessary to make the test pass (Green), and then refactoring if necessary while keeping the tests green.

This cycle ensures that you're always focused on delivering what's required and that every piece of code has a corresponding test. As we move forward, you'll continue to build on this foundation, gradually increasing the complexity of the problems and solutions while deepening your understanding of both DotNet and TDD.
