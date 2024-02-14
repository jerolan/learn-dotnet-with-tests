# Chapter 6: Classes, Methods, Interfaces, and Table-Driven Tests in .NET

This chapter delves into the foundational concepts of object-oriented programming (OOP) in .NET, focusing on classes, methods, and interfaces. Additionally, we'll explore table-driven tests, a powerful technique for organizing test cases. These concepts are essential for writing clean, maintainable, and testable code in .NET.

## Understanding Classes and Methods

In .NET, a class is a blueprint for creating objects, providing the initial values for state (member variables or properties) and implementations of behavior (member functions or methods).

### Defining a Class

A class is defined with the `class` keyword followed by its name and the class body enclosed in curly braces. Here's a simple class with one method:

```csharp
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

### Creating Instances

To use the `Calculator` class, create an instance of it:

```csharp
Calculator myCalculator = new Calculator();
int sum = myCalculator.Add(5, 3);
```

## Introducing Interfaces

An interface in .NET defines a contract that classes can implement. It specifies a set of methods (and/or properties) that implementing classes must provide. Interfaces are defined using the `interface` keyword.

### Defining an Interface

```csharp
public interface IOperation
{
    int Perform(int a, int b);
}
```

### Implementing an Interface

A class implements an interface by providing implementations for all of the interface's members:

```csharp
public class Addition : IOperation
{
    public int Perform(int a, int b)
    {
        return a + b;
    }
}
```

## Table-Driven Tests

Table-driven testing is a method where test cases are represented as rows in a table. Each row contains the input for the test and the expected output. This approach is efficient for testing functions with various inputs and outputs.

### Implementing Table-Driven Tests in .NET

In .NET, you can use table-driven tests with the help of the `[DataRow]` attribute for parameterized tests using MSTest, or `[Theory]` and `[InlineData]` attributes with xUnit.

#### Example with MSTest

Given our `Addition` class that implements the `IOperation` interface, let's write a table-driven test:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyFirstTddProject.Tests
{
    [TestClass]
    public class AdditionTests
    {
        [DataTestMethod]
        [DataRow(1, 2, 3)]
        [DataRow(-1, -1, -2)]
        [DataRow(0, 0, 0)]
        public void Perform_AddsValuesCorrectly(int a, int b, int expected)
        {
            // Arrange
            IOperation addition = new Addition();

            // Act
            int result = addition.Perform(a, b);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
```

This test method uses `[DataTestMethod]` and `[DataRow]` attributes to define a set of inputs and expected outputs, running the test method once for each row.

## Conclusion

Understanding classes, methods, and interfaces is fundamental to working with .NET and OOP. These constructs allow you to model complex problems, encapsulate behaviors, and define clear contracts between different parts of your software. Table-driven tests complement these concepts by enabling efficient and comprehensive testing of your classes and methods. By leveraging these OOP principles and testing techniques, you can build robust, maintainable, and easily testable .NET applications.
