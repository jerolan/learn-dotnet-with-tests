# Chapter 3: Working with Integers in .NET

In this chapter, we dive into working with integers in .NET, further exploring function declaration syntax, and learning new ways to improve the documentation of your code. Understanding how to work with integers and properly document your code is fundamental in building robust .NET applications with test-driven development (TDD).

## Understanding Integers in .NET

Integers in .NET are represented by various data types, each with its own range of values. The most commonly used integer types are `int` (a 32-bit signed integer) and `long` (a 64-bit signed integer). For this chapter, we'll focus on `int` for simplicity.

### Method Declaration Syntax

Declaring methods in C# involves specifying the return type, the name of the function, and any parameters. Here's a basic example of a function that adds two integers:

```csharp
public int Add(int a, int b)
{
    return a + b;
}
```

### Declaring a Class with Integer Operations

Let's create a simple class named `Calculator` that will perform integer operations. This class will serve as an example to explore function declarations and code documentation.

```csharp
public class Calculator
{
    /// <summary>
    /// Adds two integers and returns the result.
    /// </summary>
    /// <param name="a">The first integer to add.</param>
    /// <param name="b">The second integer to add.</param>
    /// <returns>The sum of the two integers.</returns>
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

## Improving Code Documentation

Documentation is crucial in making your code understandable to others and your future self. C# supports XML documentation comments, which are a powerful way to describe what your code does, the parameters it takes, and what it returns.

### XML Documentation Comments

In the `Calculator` class example above, the `Add` method includes XML documentation comments. These comments provide a summary of the method, its parameters, and its return value. This kind of documentation is especially useful in larger projects and libraries, as it can be automatically processed to generate API documentation.

### Writing Good Comments

Good comments should explain the "why" behind the code rather than the "what," which should be evident from the code itself. Keep your comments up-to-date with your code changes to avoid confusion.

## Writing Tests for Integer Operations

Testing is an integral part of TDD. Let's write a test for our `Add` method in the `Calculator` class to ensure it works as expected.

Create a new test class in your test project:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFirstTddProject;

namespace MyFirstTddProject.Tests
{
    [TestClass]
    public class CalculatorTests
    {
        [TestMethod]
        public void Add_ReturnsSumOfTwoIntegers()
        {
            // Arrange
            var calculator = new Calculator();
            int a = 5;
            int b = 7;

            // Act
            var result = calculator.Add(a, b);

            // Assert
            Assert.AreEqual(12, result);
        }
    }
}
```

### Running the Test

To run your tests, use the .NET CLI:

```
dotnet test
```

Your test should pass, indicating that the `Add` method correctly sums two integers.

## Conclusion

This chapter has introduced you to working with integers in .NET, delving into function declaration syntax, and highlighting the importance of good code documentation. By following these practices and writing tests for your code, you'll improve both the quality and maintainability of your .NET applications. As you continue with "Learn .NET with Tests," keep experimenting with different integer operations and documenting your code to deepen your understanding of C# and TDD.
