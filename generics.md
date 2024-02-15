## Generics

Generics are a powerful feature in DotNet that allow you to define classes, interfaces, and methods with placeholders for the type of data they store or operate on. This feature enables type safety without the need to specify an explicit data type, allowing for code reusability and efficiency. In this chapter, we'll explore how to use generics in DotNet, focusing on generic classes, methods, and interfaces. We'll employ Test-Driven Development (TDD) with MSTest to guide our exploration and ensure our understanding of generics.

### Understanding Generics

Generics introduce the concept of type parameters to .NET programming, letting you design classes and methods that defer the specification of one or more types until the class or method is declared and instantiated by client code. Generic collections, such as `List<T>` and `Dictionary<TKey, TValue>`, are common use cases, but generics can also be applied to create highly reusable and type-safe custom classes, interfaces, and methods.

### Setting Up Tests for Generics

To grasp the usage and benefits of generics, we'll set up tests that demonstrate their key functionalities and behaviors.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.
2. **Create a Test Class for Generics**: Consider creating a test class named `GenericsTests.cs` to contain your tests related to using generics.

### Writing Tests for Generic Classes

#### Testing a Generic Class

Let's say we have a generic repository class for managing entities in a database. We can write a test to ensure that we can add and retrieve items from the repository.

```csharp
namespace MyFirstDotNetApp.Tests;

[TestClass]
public class GenericsTests
{
    [TestMethod]
    public void GenericRepository_AddAndGetItem_ItemIsCorrectlyManaged()
    {
        // Arrange
        var repository = new GenericRepository<string>();
        var item = "Hello, Generics";

        // Act
        repository.Add(item);
        var retrievedItem = repository.GetById(0);

        // Assert
        Assert.AreEqual(item, retrievedItem);
    }
}
```

### Writing Tests for Generic Methods

#### Testing a Generic Method

Imagine we have a utility method that swaps the values of two variables. We can test this method with different types to verify its generic functionality.

```csharp
[TestMethod]
public void Swap_SwapsIntValues_ValuesAreSwapped()
{
    // Arrange
    int a = 1, b = 2;

    // Act
    Utility.Swap(ref a, ref b);

    // Assert
    Assert.AreEqual(2, a);
    Assert.AreEqual(1, b);
}
```

### Implementing the Generics to Pass the Tests

#### Implementing a Generic Class

Let's implement the `GenericRepository<T>` class used in our first test.

```csharp
namespace MyFirstDotNetApp;

public class GenericRepository<T>
{
    private readonly List<T> _items = new List<T>();

    public void Add(T item) => _items.Add(item);
    public T GetById(int id) => _items[id];
}
```

#### Implementing a Generic Method

Now, let's implement the `Swap` method used in our second test.

```csharp
public static class Utility
{
    public static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}
```

### Conclusion

This chapter has introduced generics in DotNet, showcasing their power and flexibility in creating reusable, type-safe classes, interfaces, and methods. Through TDD, we've explored how to test and implement generic functionality, ensuring a solid understanding of how to leverage generics in your applications.

Generics enhance code reusability and maintainability by allowing you to write more generalized and abstract code. As you continue to develop your DotNet skills, consider how generics can be applied to solve various programming challenges, making your solutions more robust and versatile.
