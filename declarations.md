## Declaring Variables, Constants, and Using `var` in DotNet

This chapter focuses on the fundamentals of declaring variables and constants in DotNet, along with the use of the `var` keyword for implicit typing. Understanding these concepts is essential for writing clear, maintainable, and efficient code. We'll continue our journey of exploring DotNet through Test-Driven Development (TDD) by writing tests that illustrate these concepts using MSTest.

### Variables in DotNet

Variables are named storage locations that can hold a value of a specific type. In DotNet, you must declare a variable's type explicitly unless you use the `var` keyword for local type inference.

### Constants in DotNet

Constants are immutable values which, once defined, cannot be changed. Declaring a constant in DotNet is similar to declaring a variable, except you use the `const` keyword. Constants can make your code more readable and prevent magic numbers or strings from being scattered throughout your code.

### The `var` Keyword

The `var` keyword in DotNet allows for local type inference, meaning the compiler determines the type of the variable from the value it is assigned. Using `var` can make code more concise, especially when dealing with complex types. However, its use should be balanced with readability concerns.

### Setting Up Tests for Variable and Constant Declarations

To explore these concepts, we'll set up tests that demonstrate declaring and using variables and constants, and the use of `var` for local type inference.

1. **Navigate to Your Test Project**: Make sure you're within the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Variables and Constants**: For instance, you might create a test class named `VariableTests.cs` to contain your tests.

### Writing Tests for Variables and Constants

Let's write some tests to demonstrate declaring and using variables and constants.

#### Testing Variable Declarations

```csharp
namespace MyFirstDotNetApp.Tests;

[TestClass]
public class VariableTests
{
    [TestMethod]
    public void VariableHoldsExpectedValue()
    {
        // Arrange
        int number = 5;

        // Act & Assert
        Assert.AreEqual(5, number);
    }
}
```

#### Testing Constant Declarations

```csharp
[TestMethod]
public void ConstantHoldsExpectedValue()
{
    // Arrange
    const string greeting = "Hello, World!";

    // Act & Assert
    Assert.AreEqual("Hello, World!", greeting);
}
```

### Testing the Use of `var`

```csharp
[TestMethod]
public void VarInfersCorrectType()
{
    // Arrange
    var number = 5; // Should infer int
    var word = "test"; // Should infer string

    // Act & Assert
    Assert.IsTrue(number is int);
    Assert.IsTrue(word is string);
}
```

### Implementing Code to Pass the Tests

In these examples, the tests directly demonstrate the use of variables, constants, and `var` without needing additional implementation in the main project. These tests serve to illustrate how you can declare and initialize variables and constants and how the `var` keyword infers types based on assigned values.

### Conclusion

In this chapter, you've learned about declaring variables, constants, and using the `var` keyword for local type inference in DotNet. By employing TDD, you've seen how these concepts work in practice and how they can be tested. Understanding how to effectively use variables, constants, and `var` is foundational for any DotNet developer, enabling you to write more readable, maintainable, and efficient code.

As you continue to explore DotNet, remember the importance of choosing the right approach for declaring types based on your specific needs. Whether you're defining constants to avoid magic numbers, explicitly typing variables for clarity, or using `var` for convenience, these tools are all essential parts of a DotNet developer's toolkit.
