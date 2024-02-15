## Inheritance and Interfaces in DotNet

Inheritance and interfaces are two fundamental concepts in object-oriented programming (OOP) that DotNet supports. Inheritance allows classes to inherit properties and methods from a base class, promoting code reuse and polymorphism. Interfaces, on the other hand, define contracts that classes can implement, ensuring they provide specific properties or methods. This chapter will explore the basics of inheritance and implementing interfaces, along with writing tests to verify polymorphic behavior, using Test-Driven Development (TDD) with MSTest.

### Understanding Inheritance

Inheritance enables a class (derived class) to inherit features from another class (base class). It supports code reuse and the polymorphic behavior of objects, where an object can be treated as an instance of its base class or interface.

### Understanding Interfaces

Interfaces define a contract that classes can implement. They specify a set of methods or properties that the implementing class must provide. Interfaces are useful for creating flexible and loosely-coupled systems.

### Setting Up Tests for Inheritance and Interfaces

To explore inheritance and interfaces, we'll set up tests that demonstrate their usage, focusing on polymorphism and contract implementation.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.

2. **Create Test Classes for Inheritance and Interfaces**: You might create test classes named `InheritanceTests.cs` and `InterfaceTests.cs` to contain your tests.

### Writing Tests for Inheritance

#### Testing Polymorphic Behavior

```csharp
namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class InheritanceTests
    {
        [TestMethod]
        public void AnimalSpeak_Polymorphism_CallsCorrectMethod()
        {
            // Arrange
            Animal animal = new Dog(); // Dog inherits from Animal

            // Act
            var sound = animal.Speak();

            // Assert
            Assert.AreEqual("Bark", sound);
        }
    }
}
```

### Writing Tests for Interfaces

#### Testing Interface Implementation

```csharp
[TestMethod]
public void SendMessage_UsingIMessageSender_SendsCorrectMessage()
{
    // Arrange
    IMessageSender sender = new EmailSender(); // EmailSender implements IMessageSender

    // Act
    var result = sender.SendMessage("Hello, World!");

    // Assert
    Assert.IsTrue(result.Contains("Email sent"));
}
```

### Implementing the Classes and Interfaces to Pass the Tests

Given the tests above, let's implement a simple inheritance hierarchy and an interface with a class that implements it.

#### Inheritance Implementation

Base class:

```csharp
public abstract class Animal
{
    public abstract string Speak();
}
```

Derived class:

```csharp
public class Dog : Animal
{
    public override string Speak()
    {
        return "Bark";
    }
}
```

#### Interface Implementation

Interface definition:

```csharp
public interface IMessageSender
{
    string SendMessage(string message);
}
```

Implementing class:

```csharp
public class EmailSender : IMessageSender
{
    public string SendMessage(string message)
    {
        // Simulate sending an email
        return $"Email sent with message: {message}";
    }
}
```

### Conclusion

In this chapter, you've explored the concepts of inheritance and interfaces in DotNet. By leveraging TDD, you've written tests to demonstrate polymorphic behavior through inheritance and ensured classes adhere to contracts defined by interfaces. These concepts are crucial for building well-structured, reusable, and maintainable code in object-oriented programming.

As you continue your DotNet journey, experiment further with these concepts, exploring more complex inheritance structures and interface implementations. This foundation will be invaluable as you tackle more advanced OOP patterns and design principles in your future projects.
