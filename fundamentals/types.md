## Types and Variables

In this chapter, we'll dive deeper into the DotNet language by exploring its built-in types and variables. Understanding types and how to manipulate them is fundamental to programming in DotNet. As with the previous chapter, we'll approach learning these concepts through Test-Driven Development (TDD) using MSTest. We'll write tests first to explore the behavior of different types and variables, then implement code to pass those tests.

### Basic Types in DotNet

DotNet provides a range of built-in types that represent integers, floating-point numbers, characters, boolean values, and more. Here, we'll focus on a few fundamental types:

- **Integers (`int`)**: Represents whole numbers.
- **Floating-point numbers (`float`, `double`)**: Represent real numbers with fractional parts.
- **Boolean (`bool`)**: Represents true or false values.
- **Characters (`char`)**: Represents a single character.
- **Strings (`string`)**: Represents a sequence of characters.

### Setting Up Tests for Types and Variables

We'll start by writing tests to explore operations with these types. Create a new test class for each type to keep your tests organized.

1. **Navigate to Your Test Project**: Ensure you're in the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Integer Operations**: For instance, to explore integer operations, you might create a test class named `IntegerTests.cs` and write tests within it to understand how integer arithmetic works in DotNet.

### Writing Your First Tests

Let's write some tests for integer arithmetic to understand how to work with `int` types in DotNet.

#### Testing Integer Operations

```csharp
namespace MyFirstDotNetApp.Tests;

[TestClass]
public class IntegerTests
{
    [TestMethod]
    public void IntegerAddition_ReturnsCorrectResult()
    {
        // Arrange
        int numberOne = 5;
        int numberTwo = 3;

        // Act
        int sum = numberOne + numberTwo;

        // Assert
        Assert.AreEqual(8, sum);
    }
}
```

### Implementing Code to Pass the Tests

For the test above, no additional implementation in the main project is necessary since we're testing built-in operations. However, as you write more complex tests (for example, testing methods that manipulate strings or perform calculations), you'll implement methods in your main project to perform these operations and then call those methods from your tests.

### Testing Floating-Point Arithmetic

Floating-point numbers are a bit more complex due to precision. When writing tests involving floating-point arithmetic, consider precision in your assertions.

```csharp
[TestMethod]
public void FloatingPointDivision_ReturnsApproximateResult()
{
    // Arrange
    double numerator = 10;
    double denominator = 3;

    // Act
    double result = numerator / denominator;

    // Assert
    Assert.AreEqual(3.33, result, 0.01); // Allow a small margin of error
}
```

### Testing Boolean Logic

Boolean logic is fundamental to control flow in programming. Write tests to understand boolean operations and conditional logic.

```csharp
[TestMethod]
public void BooleanLogic_AndOperator_ReturnsTrueOnlyIfBothTrue()
{
    // Arrange
    bool firstCondition = true;
    bool secondCondition = false;

    // Act
    bool result = firstCondition && secondCondition;

    // Assert
    Assert.IsFalse(result);
}
```

### Conclusion

In this chapter, you've started to explore the fundamental types in DotNet and how to manipulate them through TDD. By writing tests first, you're not only learning about the types and variables but also practicing TDD principles. This approach ensures you understand the behavior of code you write and fosters good habits of writing testable, maintainable code.

As you become more comfortable with basic types, you can start exploring more complex data structures and their operations, always guided by the TDD methodology. The next chapters will delve into control flow, collections, and more advanced features of the DotNet language, each time building on the foundation you've set up here.
