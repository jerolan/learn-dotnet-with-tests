# Chapter 4: Iteration and Benchmarking in .NET

In this chapter, we focus on the concept of iteration in .NET using `for` loops, a fundamental part of programming that allows us to execute a block of code multiple times. Additionally, we'll introduce the concept of benchmarking to measure the performance of our iterative code, ensuring our applications are not only correct but also efficient.

## Iteration with `for` Loops

Iteration, or looping, enables us to perform repetitive tasks efficiently in our programs. The `for` loop is one of the most common loop constructs in C#. Let's explore its syntax and usage with a practical example.

### `for` Loop Syntax

The `for` loop consists of three parts: initialization, condition, and increment:

```csharp
for (initialization; condition; increment)
{
    // Block of code to be executed
}
```

### Example: Summing an Array of Integers

Suppose we have an array of integers and we want to calculate the sum of all elements in the array. This is a perfect use case for a `for` loop.

First, following TDD principles, we write a test for a method that sums an array of integers:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyFirstTddProject;

namespace MyFirstTddProject.Tests
{
    [TestClass]
    public class ArrayCalculatorTests
    {
        [TestMethod]
        public void Sum_ReturnsTotalOfArrayElements()
        {
            // Arrange
            ArrayCalculator calculator = new ArrayCalculator();
            int[] numbers = new int[] { 1, 2, 3, 4, 5 };
            int expectedSum = 15;

            // Act
            int result = calculator.Sum(numbers);

            // Assert
            Assert.AreEqual(expectedSum, result);
        }
    }
}
```

Then, we implement the `Sum` method in our `ArrayCalculator` class:

```csharp
public class ArrayCalculator
{
    public int Sum(int[] numbers)
    {
        int sum = 0;
        for (int i = 0; i < numbers.Length; i++)
        {
            sum += numbers[i];
        }
        return sum;
    }
}
```

After running `dotnet test`, we should see that our test passes, demonstrating a basic use of `for` loops for iteration.

## Conclusion

Iteration are crucial concepts in software development. By understanding how to implement `for` loops in C#, you can handle repetitive tasks efficiently in your code. As you continue to explore .NET with tests, remember to apply these concepts and tools to ensure your code is both robust and efficient.
