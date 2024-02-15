## Structs and Records in DotNet

In this chapter, we'll explore two important types in DotNet: structs and records. Both are value types that serve distinct purposes in application development. Structs are typically used for small data structures that contain primarily data that is not intended to be modified after creation. Records, introduced in C# 9.0, are perfect for immutable data models and provide built-in functionality for value equality. As with previous concepts, we'll employ Test-Driven Development (TDD) using MSTest to understand how to effectively use structs and records.

### Understanding Structs

A struct (short for structure) is a value type that is typically used to encapsulate small groups of related variables. Unlike classes, structs are value types, and instances of a struct are copied when they are passed to methods or assigned to other variables.

### Understanding Records

Records are a type of reference that is designed to be immutable; once a record is created, its data cannot be changed. Records provide built-in support for value-based equality checks, meaning two record instances are considered equal if all their values are equal.

### Setting Up Tests for Structs and Records

To explore structs and records, we'll write tests that demonstrate their usage and behaviors, focusing on their key characteristics.

1. **Navigate to Your Test Project**: Ensure you're in the `MyFirstDotNetApp.Tests` directory.

2. **Create Test Classes**: Create separate test classes for structs and records, such as `StructTests.cs` and `RecordTests.cs`.

### Writing Tests for Structs

#### Testing Struct Behavior

```csharp
namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class StructTests
    {
        [TestMethod]
        public void StructAssignment_CreatesCopy()
        {
            // Arrange
            var originalStruct = new SimpleStruct { Number = 1, Text = "Original" };
            var copiedStruct = originalStruct; // This creates a copy

            // Act
            copiedStruct.Number = 2; // Modifying the copy shouldn't affect the original

            // Assert
            Assert.AreEqual(1, originalStruct.Number); // Original should remain unchanged
        }
    }
}
```

### Implementing the Struct to Pass the Test

Define a simple struct that matches the expectations of your test.

```csharp
public struct SimpleStruct
{
    public int Number { get; set; }
    public string Text { get; set; }
}
```

### Writing Tests for Records

#### Testing Record Equality

```csharp
namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class RecordTests
    {
        [TestMethod]
        public void RecordEquality_ByValue()
        {
            // Arrange
            var record1 = new SimpleRecord("Name", 1);
            var record2 = new SimpleRecord("Name", 1);

            // Act & Assert
            Assert.AreEqual(record1, record2); // Records are equal if their values are equal
        }
    }
}
```

### Implementing the Record to Pass the Test

Define a simple record that matches the expectations of your test. Records are defined similarly to classes but use the `record` keyword.

```csharp
public record SimpleRecord(string Name, int Number);
```

### Conclusion

In this chapter, you've learned about structs and records in DotNet, two powerful constructs for working with data. Structs, being value types, are best used for small data structures that do not require inheritance or extensive behavior. Records offer a concise syntax for creating immutable data models and come with built-in support for value-based equality, making them ideal for functional programming patterns.

By using TDD to explore structs and records, you've not only gained an understanding of how to define and use these types but also how to write tests that ensure they behave as expected. This approach reinforces the principles of immutability, value equality, and the differences between value types and reference types in DotNet.
