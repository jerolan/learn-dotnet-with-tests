## `with` Keyword for Records

In DotNet, the `with` keyword is introduced alongside records to facilitate non-destructive mutation. This feature allows you to create a new record instance by copying an existing record while changing one or more properties in the process. This chapter will explore how to use the `with` keyword effectively, demonstrating its value in creating immutable data structures that are easy to work with. We'll continue our practice of Test-Driven Development (TDD) using MSTest to guide our exploration and understanding.

### Understanding the `with` Keyword

The `with` keyword is used with record types to create a new record instance that is a copy of an existing instance, except with some properties modified. This supports the concept of immutability in records by providing a straightforward way to "change" an immutable object without actual modification of the original object.

### Setting Up Tests for the `with` Keyword

To understand how the `with` keyword works and to see its benefits, we'll set up tests that demonstrate creating modified copies of records.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` directory.

2. **Extend the RecordTests Class**: Add tests to the existing `RecordTests.cs` class or create a new test class if you prefer, focusing on the use of the `with` keyword.

### Writing Tests for the `with` Keyword

#### Testing Non-Destructive Mutation

```csharp
[TestMethod]
public void WithKeyword_CreatesModifiedCopy()
{
    // Arrange
    var originalRecord = new SimpleRecord("John", 30);

    // Act
    var modifiedRecord = originalRecord with { Age = 31 };

    // Assert
    Assert.AreEqual("John", originalRecord.Name); // Original remains unchanged
    Assert.AreEqual(30, originalRecord.Age); // Original remains unchanged
    Assert.AreEqual("John", modifiedRecord.Name); // Name is copied
    Assert.AreEqual(31, modifiedRecord.Age); // Age is modified
}
```

### Implementing the Record to Pass the Test

Ensure you have a `PersonRecord` that matches the expectations of your tests. If not already defined from previous chapters, define it now.

```csharp
public record PersonRecord(string Name, int Age);
```

This record definition allows for the `with` keyword to be used effectively, demonstrating the simplicity of creating modified copies of immutable objects.

### Understanding Immutability and Modification

The `with` keyword underlines the principle of immutability in functional programming, which is increasingly influential in C# development. Immutability refers to the idea that once an object is created, it cannot be changed. This concept is valuable for several reasons, including thread safety, predictability, and ease of reasoning about code. However, immutability can introduce challenges when changes are needed. The `with` keyword addresses this by allowing for the creation of modified copies, thus maintaining immutability while providing flexibility.

### Conclusion

The introduction of the `with` keyword for records in DotNet represents a significant step forward in supporting immutable data structures within the language. By enabling non-destructive mutation, it allows developers to work with immutable objects in a more natural and efficient manner. This chapter has shown how to use TDD to explore and understand the use of the `with` keyword, reinforcing the benefits of immutability and the practicalities of working with records. As you continue to develop your skills in DotNet, leveraging features like records and the `with` keyword will help you write more robust, maintainable, and thread-safe applications.
