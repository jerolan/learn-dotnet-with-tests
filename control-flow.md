## Control Flow

Control flow is a fundamental concept in programming that allows you to dictate how your code executes under different conditions or as it repeats operations. In DotNet, control flow structures enable you to perform decision-making, looping, and branching in your applications. This chapter introduces you to these structures, emphasizing Test-Driven Development (TDD) with MSTest to explore and understand their behavior.

### Understanding Control Flow Structures

Control flow in DotNet is managed using various statements and constructs, including:

- **Conditional Statements (`if`, `else if`, `else`, `switch`)**: Allow you to execute different code blocks based on conditions.
- **Looping Statements (`for`, `foreach`, `while`, `do-while`)**: Enable you to execute a block of code repeatedly, based on a condition or iterating over a collection.
- **Jump Statements (`break`, `continue`, `return`)**: Control the flow by transferring control to another point in the code.

### Setting Up Tests for Control Flow

To understand these control flow structures deeply, we'll write tests that demonstrate their use and behavior. This approach not only helps in learning these constructs but also in practicing how to test code that uses them.

1. **Navigate to Your Test Project**: Ensure you're within the `MyFirstDotNetApp.Tests` project directory.

2. **Create a Test Class for Control Flow**: You might create a test class named `ControlFlowTests.cs` to house your tests related to control flow structures.

### Writing Tests for Conditional Statements

Let's start by writing tests for conditional statements, using the `if` statement as an example.

#### Testing `if` Statements

```csharp
namespace MyFirstDotNetApp.Tests;

[TestClass]
public class ControlFlowTests
{
    [TestMethod]
    public void IfStatement_ExecutesTrueBranch_Correctly()
    {
        // Arrange
        bool condition = true;
        string result = "";

        // Act
        if (condition)
        {
            result = "True Branch";
        }
        else
        {
            result = "False Branch";
        }

        // Assert
        Assert.AreEqual("True Branch", result);
    }
}
```

### Writing Tests for Looping Statements

Now, let's write a test to understand looping statements, focusing on the `for` loop.

#### Testing `for` Loops

```csharp
[TestMethod]
public void ForLoop_ExecutesMultipleTimes_Correctly()
{
    // Arrange
    int sum = 0;

    // Act
    for (int i = 1; i <= 5; i++)
    {
        sum += i;
    }

    // Assert
    Assert.AreEqual(15, sum);
}
```

### Writing Tests for Jump Statements

Jump statements can be tricky to test directly since they affect the flow of execution. However, you can test the outcomes of using these statements. For example, a `return` statement inside a method can be tested by calling the method and verifying its output.

#### Testing `return` Statements

```csharp
[TestMethod]
public void ReturnStatement_ReturnsEarlyIfConditionIsTrue()
{
    // Arrange & Act
    var result = ControlFlowExamples.ReturnEarlyIfTrue(true);

    // Assert
    Assert.AreEqual("Returned Early", result);
}
```

### Implementing Code to Pass the Tests

In these tests, you directly interact with control flow structures mostly without needing additional implementation in the main project for the basics. As you progress to more complex scenarios, you'll implement methods that use these control flow structures, which you'll then test to ensure they behave as expected.

If we run these tests now, they will fail because `ReturnEarlyIfTrue` is not implemented yet. To ensure the tests we've discussed in the chapter on Control Flow in DotNet not only compile but also pass, let's delve into how we might implement the missing parts, specifically focusing on the scenario for testing jump statements with a method example `ReturnEarlyIfTrue`.

### Implementing a Class with a Method for Testing Jump Statements

For the jump statements, particularly testing the behavior of `return` statements, we'll need a simple class with a method that demonstrates this behavior. This method will return different values based on the input, allowing us to test the effect of a `return` statement within a conditional structure.

#### Step 1: Creating the Class

First, we need to create a class that will contain our method. Let's call this class `ControlFlowExamples` and define it in the main project (`MyFirstDotNetApp`).

#### Step 2: Implementing the `ReturnEarlyIfTrue` Method

Inside the `ControlFlowExamples` class, we'll implement the `ReturnEarlyIfTrue` method, which takes a boolean parameter and returns a string based on the parameter's value. This method demonstrates the use of an `if` statement combined with a `return` statement to control the flow of execution.

```csharp
namespace MyFirstDotNetApp;

public class ControlFlowExamples
{
    public static string ReturnEarlyIfTrue(bool condition)
    {
        if (condition)
        {
            return "Returned Early";
        }

        return "Returned at End";
    }
}
```

#### Step 3: Adjusting the Test to Use the Implemented Method

Now, we'll adjust our test to use this newly created method. This involves calling the `ReturnEarlyIfTrue` method with a boolean value. The test will verify that the method's return value matches our expectations based on the input.

```csharp
[TestMethod]
public void ReturnStatement_ReturnsEarlyIfConditionIsTrue()
{
    // Arrange
    var controlFlowExamples = new ControlFlowExamples();

    // Act
    var resultTrueCondition = controlFlowExamples.ReturnEarlyIfTrue(true);
    var resultFalseCondition = controlFlowExamples.ReturnEarlyIfTrue(false);

    // Assert
    Assert.AreEqual("Returned Early", resultTrueCondition);
    Assert.AreEqual("Returned at End", resultFalseCondition);
}
```

In this revised test, we're not only testing the behavior when the condition is true, but we've also added a check for when the condition is false to ensure our method behaves correctly in both scenarios.

### Conclusion

In this chapter, you've learned about control flow structures in DotNet, including conditional statements, looping statements, and jump statements. By writing tests first, you've practiced TDD and seen how these constructs work in practice. Understanding and effectively using control flow is essential for creating complex and dynamic applications in DotNet.

As you continue your journey in learning DotNet with tests, remember that control flow structures are the building blocks for dictating how your code executes. Mastery of these concepts will greatly enhance your ability to write robust, efficient, and maintainable code.
