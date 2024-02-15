## Collections

Collections are fundamental to storing, managing, and manipulating groups of objects in DotNet. They offer various structures, each with its own advantages, to efficiently handle data sets. This chapter explores the core collection types provided by the .NET framework, such as Lists, Dictionaries, Queues, and Stacks, and how to use them effectively. We'll employ Test-Driven Development (TDD) with MSTest to understand these collections' behaviors and functionalities.

### Understanding Collections

DotNet collections are part of the System.Collections and System.Collections.Generic namespaces, offering both non-generic and generic versions. Generic collections are type-safe, ensuring that only objects of a specified type are stored in the collection, providing better performance and reducing runtime errors.

### Setting Up Tests for Collections

To explore the usage and features of different collections, we'll set up tests that demonstrate their key functionalities and behaviors.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.
2. **Create Test Classes for Collections**: Create a test class named `CollectionsTests.cs` to contain your tests related to using collections.

### Writing Tests for Lists

Create the base test class for collections:

```csharp
namespace MyFirstDotNetApp.Tests;

using System.Collections.Generic;

[TestClass]
public class CollectionsTests
{
    // Tests for different collection types will be added here
}
```

#### Testing List Additions and Retrievals

```csharp
[TestMethod]
public void List_AddAndRetrieveItems_ItemsAreCorrectlyManaged()
{
    // Arrange
    var names = new List<string>();

    // Act
    names.Add("Alice");
    names.Add("Bob");

    // Assert
    Assert.AreEqual(2, names.Count);
    Assert.AreEqual("Alice", names[0]);
    Assert.AreEqual("Bob", names[1]);
}
```

### Writing Tests for Dictionaries

#### Testing Dictionary Key-Value Pairs

```csharp
[TestMethod]
public void Dictionary_AddAndRetrieveByKey_ValueIsCorrect()
{
    // Arrange
    var capitals = new Dictionary<string, string>();

    // Act
    capitals.Add("France", "Paris");
    capitals.Add("Japan", "Tokyo");

    // Assert
    Assert.AreEqual("Paris", capitals["France"]);
    Assert.AreEqual("Tokyo", capitals["Japan"]);
}
```

### Writing Tests for Queues

#### Testing Queue Enqueue and Dequeue Operations

```csharp
[TestMethod]
public void Queue_EnqueueAndDequeue_ItemsAreCorrectlyProcessed()
{
    // Arrange
    var queue = new Queue<int>();

    // Act
    queue.Enqueue(1);
    queue.Enqueue(2);
    int firstOut = queue.Dequeue();

    // Assert
    Assert.AreEqual(1, firstOut); // FIFO order
    Assert.AreEqual(1, queue.Count); // One item should be left
}
```

### Writing Tests for Stacks

#### Testing Stack Push and Pop Operations

```csharp
[TestMethod]
public void Stack_PushAndPop_ItemsAreCorrectlyProcessed()
{
    // Arrange
    var stack = new Stack<int>();

    // Act
    stack.Push(1);
    stack.Push(2);
    int lastOut = stack.Pop();

    // Assert
    Assert.AreEqual(2, lastOut); // LIFO order
    Assert.AreEqual(1, stack.Count); // One item should be left
}
```

### Conclusion

This chapter has explored the use of collections in DotNet, highlighting how different types of collections can be utilized to manage data effectively. Through TDD, we've seen how to test the behavior and functionality of Lists, Dictionaries, Queues, and Stacks, ensuring our understanding of these structures.

Collections are a vital part of application development in DotNet, providing the tools needed to store and manipulate groups of objects efficiently. As you continue to build your DotNet applications, consider which collection types best suit your needs, taking into account factors like data access patterns, performance requirements, and the nature of the data you're working with.
