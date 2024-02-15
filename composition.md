## Composition Over Inheritance

In this chapter, we delve into the principle of "composition over inheritance," a fundamental design guideline that favors composing objects to achieve code reusability and flexibility over the traditional inheritance hierarchy. While inheritance is a powerful tool in object-oriented programming (OOP), it can lead to rigid and fragile designs when overused. Composition, by assembling objects to achieve more complex functionalities, offers greater flexibility and encapsulation. We'll explore this concept through Test-Driven Development (TDD) with MSTest, illustrating how composition can be used effectively in DotNet.

### Understanding Composition Over Inheritance

Composition involves building complex objects from smaller objects, promoting loose coupling and adherence to the Single Responsibility Principle. It allows for more granular reuse and can simplify changes since they tend to be localized to the component being modified.

### Setting Up Tests for Composition Over Inheritance

To explore composition, we'll set up tests that demonstrate how objects can be composed to achieve the desired functionality, contrasting with an inheritance-based approach.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.
2. **Create a Test Class for Composition**: Consider creating a test class named `CompositionTests.cs` to contain your tests related to using composition.

### Writing Tests for Composition

#### Testing Functionality Through Composition

```csharp
namespace MyFirstDotNetApp.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using MyFirstDotNetApp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompositionTests
    {
        [TestMethod]
        public void RobotPerformsTasks_ThroughComposition_HasExpectedBehavior()
        {
            // Arrange
            var robot = new Robot(new List<IRobotTask> { new SpeakTask(), new MoveTask() });

            // Act
            var speakResult = robot.PerformTask("Speak");
            var moveResult = robot.PerformTask("Move");

            // Assert
            Assert.AreEqual("Speaking", speakResult);
            Assert.AreEqual("Moving", moveResult);
        }
    }
}
```

### Implementing Composition to Pass the Tests

Given the test above, let's implement a `Robot` class that uses composition to perform tasks, along with an `IRobotTask` interface and task implementations.

#### Interface and Task Implementations

Interface for robot tasks:

```csharp
public interface IRobotTask
{
    string TaskName { get; }
    string PerformTask();
}
```

Task implementations:

Create two classes, `SpeakTask` and `MoveTask`, that implement the `IRobotTask` interface. Wrap each file in a folder named `RobotTasks` to keep the project organized.

```csharp
public class SpeakTask : IRobotTask
{
    public string TaskName => "Speak";
    public string PerformTask() => "Speaking";
}

public class MoveTask : IRobotTask
{
    public string TaskName => "Move";
    public string PerformTask() => "Moving";
}
```

#### Robot Class Using Composition

```csharp
public class Robot
{
    private readonly Dictionary<string, IRobotTask> _tasks;

    public Robot(IEnumerable<IRobotTask> tasks)
    {
        _tasks = tasks.ToDictionary(task => task.TaskName);
    }

    public string PerformTask(string taskName)
    {
        if (_tasks.TryGetValue(taskName, out var task))
        {
            return task.PerformTask();
        }
        return "Task not found";
    }
}
```

### Conclusion

This chapter has introduced the concept of composition over inheritance, illustrating how composition can be leveraged in DotNet to build flexible and maintainable systems. By composing objects rather than relying on rigid inheritance hierarchies, you can achieve a high degree of code reuse and simplicity.

Using TDD to explore composition helps underscore its benefits and practical applications. As you continue to design and develop in DotNet, consider how composition can be used to enhance your projects, keeping in mind that the best solutions often come from a balance of inheritance, composition, and other design patterns.
