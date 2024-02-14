# Chapter 2: Hello, World - The Basics of .NET with Tests

In this chapter, we'll embark on writing our first .NET program and accompanying test, focusing on fundamental concepts such as declaring variables and constants, control flow with if/else statements and switch cases, and leveraging sub-tests and closures in our tests. This hands-on approach will not only introduce you to the syntax of C# (the primary language we'll use for .NET examples) but also to the test-driven development (TDD) process.

## Writing Your First .NET Program

Let's start with the classic "Hello, World" program. This simple exercise will introduce you to the structure of a .NET console application and the process of compiling and running your code.

### 1. Creating the Application

You should already have a .NET project set up from Chapter 1. Navigate to your project's root directory and open the `Program.cs` file. Replace its contents with the following code:

```csharp
using System;

namespace MyFirstTddProject
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
```

### 2. Running Your Application

To run your application, use the .NET CLI with the following command in your terminal:

```
dotnet run --project MyFirstTddProject
```

You should see "Hello, World!" printed to the console. Congratulations, you've just run your first .NET program!

## Declaring Variables and Constants

In C#, variables and constants hold data that your program can manipulate. Variables are mutable, whereas constants are immutable.

### Variables

```csharp
string greeting = "Hello";
int number = 42;
```

### Constants

```csharp
const string WORLD = "World";
```

## Control Flow

### If/Else Statements

```csharp
if (number > 0)
{
    Console.WriteLine(greeting + " Positive Number");
}
else
{
    Console.WriteLine(greeting + " Not Positive Number");
}
```

### Switch Statement

```csharp
switch (greeting)
{
    case "Hello":
        Console.WriteLine("Greeted with Hello");
        break;
    default:
        Console.WriteLine("Greeted differently");
        break;
}
```

## Writing Your First Test

Now, let's write a test for a simple function that returns a greeting message. First, add a new class to your project:

```csharp
// Inside the MyFirstTddProject project
public class Greeter
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}
```

### Setting Up the Test

Navigate to your test project (`MyFirstTddProject.Tests`) and create a new test file, `GreeterTests.cs`. Add the following code:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFirstTddProject;

namespace MyFirstTddProject.Tests
{
    [TestClass]
    public class GreeterTests
    {
        [TestMethod]
        public void Greet_ReturnsHelloWithName()
        {
            // Arrange
            var greeter = new Greeter();
            var name = "World";

            // Act
            var result = greeter.Greet(name);

            // Assert
            Assert.AreEqual("Hello, World!", result);
        }
    }
}
```

### Running the Test

Use the .NET CLI to run your tests:

```
dotnet test
```

Your test should pass, indicating that the `Greet` method works as expected.

## Sub-test Syntax and Closures

C# doesn't have a built-in concept of sub-tests like some other languages do. However, you can achieve similar functionality by using test cases or dynamically generating tests with closures for parameterized testing.

## Conclusion

You've now written your first .NET program and accompanying test, getting a taste of variables, constants, control flow, and the basics of the MSTest framework. As you progress, these foundational skills will serve as the building blocks for more complex applications and testing scenarios. Remember, the key to mastering TDD and .NET development is practice and continuous learning. Keep experimenting with different types of tests and exploring .NET's rich feature set to build your expertise.
