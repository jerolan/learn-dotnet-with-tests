## Decimals

In this chapter, we dive into the `decimal` type in DotNet, which is crucial for situations requiring high precision, such as financial calculations. Unlike `float` and `double`, the `decimal` type has a greater precision and a smaller range, making it ideal for calculations where accuracy of decimal points is essential. We'll explore the `decimal` type through Test-Driven Development (TDD) using MSTest, writing tests first to understand its behavior and then implementing code to satisfy those tests.

### Understanding the Decimal Type

The `decimal` type in DotNet is a 128-bit data type suitable for financial and monetary calculations. Its high precision and reduced chance of rounding errors make it the preferred choice over `float` and `double` for such use cases.

### Setting Up Tests for Decimal Operations

To begin exploring `decimal` operations, we'll set up a test class specifically for decimal arithmetic. This approach keeps our tests organized and focused on the unique aspects of working with `decimal`.

1. **Navigate to Your Test Project**: Ensure you're in the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Decimal Operations**: For instance, to explore decimal operations, you might create a test class named `DecimalTests.cs` and write tests within it to understand how decimal arithmetic works in DotNet.

### Writing Your First Decimal Tests

Let's write some tests to explore basic arithmetic operations using the `decimal` type.

#### Testing Decimal Addition

```csharp
namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class DecimalTests
    {
        [TestMethod]
        public void DecimalAddition_ReturnsCorrectResult()
        {
            // Arrange
            decimal numberOne = 5.5m;
            decimal numberTwo = 2.5m;

            // Act
            decimal sum = numberOne + numberTwo;

            // Assert
            Assert.AreEqual(8.0m, sum);
        }
    }
}
```

### Implementing Code to Pass the Tests

For these tests, no additional implementation in the main project is necessary since we're testing built-in operations of the `decimal` type. However, these examples serve as a foundation for when you need to perform more complex operations or business logic involving decimals.

### Testing Decimal Precision

One of the key features of the `decimal` type is its precision. It's important to understand how it behaves in operations that could potentially lose precision.

```csharp
[TestMethod]
public void DecimalMultiplication_HighPrecision_ReturnsAccurateResult()
{
    // Arrange
    decimal price = 19.95m;
    decimal quantity = 3m;

    // Act
    decimal total = price * quantity;

    // Assert
    Assert.AreEqual(59.85m, total);
}
```

### Handling Rounding

When working with `decimal` types, you might need to round the result to a certain number of decimal places. It's crucial to understand how to apply rounding correctly to avoid unexpected results.

```csharp
[TestMethod]
public void DecimalRounding_RoundsToTwoDecimalPlaces_CorrectlyRoundsUp()
{
    // Arrange
    decimal originalValue = 10.555m;

    // Act
    decimal roundedValue = Math.Round(originalValue, 2);

    // Assert
    Assert.AreEqual(10.56m, roundedValue);
}
```

### Conclusion

In this chapter, you've learned how to work with the `decimal` type in DotNet for high-precision arithmetic operations. By using TDD, you've explored its behavior in various scenarios, including addition, multiplication, and rounding. This approach not only helps you understand the `decimal` type but also reinforces good testing practices, ensuring that your code behaves as expected in precision-critical applications.

As you continue to build your DotNet skills, remember the importance of choosing the right type for the job. In cases requiring precise decimal arithmetic, such as financial applications, the `decimal` type is often the best choice. Keep practicing TDD to deepen your understanding of DotNet and to develop robust, well-tested applications.
