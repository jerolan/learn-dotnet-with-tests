## Classes and Objects

In this chapter, we delve into the foundational concepts of Object-Oriented Programming (OOP) within the DotNet ecosystem: classes and objects. Understanding how to define and use classes and objects is crucial for developing structured and maintainable code. We'll approach learning these concepts through Test-Driven Development (TDD), using MSTest to write tests that guide our implementation.

### Understanding Classes and Objects

In DotNet, a class is a blueprint for creating objects (instances of a class). A class defines properties (attributes) and methods (functions) that its objects will have. An object is an instance of a class, created in memory, with its own set of values for the properties defined by the class.

### Setting Up Tests for Classes and Objects

To explore the concepts of classes and objects, we'll set up tests that illustrate creating and interacting with them.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Classes and Objects**: You might create a test class named `PersonTests.cs` to contain your tests related to a `Person` class that we will define.

### Writing Your First Class and Object Tests

Let's start by defining a simple `Person` class in our main project, then write tests to interact with instances of this class.

```csharp
using MyFirstDotNetApp;

namespace MyFirstDotNetApp.Tests
{
    [TestClass]
    public class PersonTests
    {
        [TestMethod]
        public void CreatingPerson_SetsNameAndAgeProperties_Correctly()
        {
            // Arrange & Act
            var person = new Person { Name = "John Doe", Age = 30 };

            // Assert
            Assert.AreEqual("John Doe", person.Name);
            Assert.AreEqual(30, person.Age);
        }
    }
}
```

#### Defining the Person Class

First, define a `Person` class in your main project (`MyFirstDotNetApp`). This class will have a couple of properties and a method.

```csharp
namespace MyFirstDotNetApp
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public string IntroduceYourself()
        {
            return $"Hello, my name is {Name} and I am {Age} years old.";
        }
    }
}
```

#### Run the Tests

Ensure that your tests are passing by running the tests in your test project.

```bash
dotnet test
```

##### Testing Method Behavior

```csharp
[TestMethod]
public void IntroduceYourself_ReturnsCorrectIntroduction()
{
    // Arrange
    var person = new Person { Name = "Jane Doe", Age = 25 };

    // Act
    var introduction = person.IntroduceYourself();

    // Assert
    Assert.AreEqual("Hello, my name is Jane Doe and I am 25 years old.", introduction);
}
```

### Conclusion

In this chapter, you've learned how to define classes and create objects in DotNet. By using TDD, you've written tests that guide the development of a simple `Person` class, allowing you to practice defining properties and methods, and creating instances of the class. This approach reinforces the concept of classes as blueprints for objects and highlights the role of objects as containers of state and behavior.

As you continue your journey in DotNet, keep exploring more complex class designs, including inheritance, encapsulation, and polymorphism, to build robust and flexible applications. Remember, the principles of OOP combined with TDD practices set a strong foundation for writing high-quality, maintainable code.
