## Properties and Methods

This chapter dives into the core components of classes and records in DotNet: properties and methods. Properties in DotNet are members that provide a flexible mechanism to read, write, or compute the values of private fields. Methods, on the other hand, are blocks of code that perform operations. Both are pivotal in defining the behavior and data encapsulation of your types. We will continue our exploration using Test-Driven Development (TDD) with MSTest to guide our understanding and application of these concepts.

### Understanding Properties

Properties in DotNet act as public accessors for private data. They can include both a `get` and a `set` accessor, which control how values are read and modified. Properties can be read-only, write-only (less common), or read-write. They can also be auto-implemented, where the compiler automatically generates a private, anonymous backing field.

### Understanding Methods

Methods define actions that an object can take or operations that can be performed on it. They can accept parameters, return values, and encapsulate logic that operates on the class's internal state or perform operations unrelated to specific objects.

### Setting Up Tests for Properties and Methods

To effectively learn about properties and methods, we'll write tests that illustrate their behavior and usage in classes and records.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Properties and Methods**: Consider creating a test class named `ProductTests.cs` to contain tests related to a hypothetical `Product` class that we will define.

### Writing Tests for Properties

#### Testing Property Getters and Setters

```csharp
namespace MyFirstDotNetApp.Tests;

[TestClass]
public class ProductTests
{
    [TestMethod]
    public void ProductName_PropertyWorksAsExpected()
    {
        // Arrange
        var product = new Product();
        var expectedName = "Gadget";

        // Act
        product.Name = expectedName; // Testing set
        var actualName = product.Name; // Testing get

        // Assert
        Assert.AreEqual(expectedName, actualName);
    }
}
```

### Writing Tests for Methods

#### Testing Method Execution

```csharp
[TestMethod]
public void CalculateDiscount_PriceAboveThreshold_AppliesDiscount()
{
    // Arrange
    var product = new Product { Price = 200.00m };
    var expectedPriceAfterDiscount = 180.00m; // Assuming a 10% discount

    // Act
    product.ApplyDiscount(10); // Apply a 10% discount

    // Assert
    Assert.AreEqual(expectedPriceAfterDiscount, product.Price);
}
```

### Implementing the Class to Pass the Tests

Given the tests above, let's implement a simple `Product` class with properties for `Name` and `Price`, and a method to apply a discount.

```csharp
namespace MyFirstDotNetApp;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }

    public void ApplyDiscount(decimal percentage)
    {
        var discountAmount = Price * (percentage / 100);
        Price -= discountAmount;
    }
}
```

### Conclusion

In this chapter, you've learned how to define and use properties and methods in DotNet classes through practical examples. By employing TDD, you've seen how to test the behavior of properties and methods, ensuring they work as expected. Properties and methods are fundamental to encapsulating data and behavior within your classes, making your applications more modular, maintainable, and easy to understand.

As you continue to develop your DotNet skills, experiment with different types of properties (such as computed properties or properties with private setters) and explore the vast range of operations that methods can perform. This foundation will serve you well as you move on to more advanced topics and build more complex applications.
