# Hello, .NET World!

In this chapter, we'll dive into writing our first piece of code and test in .NET, adhering to the principles of Test-Driven Development (TDD). We'll start with something simple: a function that returns "Hello, World!" This will introduce you to the basics of .NET programming and testing with MSTest.

## Getting Started

Before we begin, ensure you have the .NET SDK and Visual Studio Code set up as described in the previous chapter.

## Writing Your First Test

1. **Create a Test Project**: If you haven't already, create a new directory for your project. Inside this directory, create a test project by running:

   ```
   dotnet new mstest -n HelloWorldTests
   ```

   Navigate into your test project folder:

   ```
   cd HelloWorldTests
   ```

2. **Add Your Test Class**: Open the project in Visual Studio Code. You'll find a file named `UnitTest1.cs`—let's rename it to `GreeterTests.cs` to reflect what we're testing. Now, open `GreeterTests.cs` and replace its contents with the following code:

   ```csharp
   using HelloWorld;

   namespace HelloWorldTests
   {
      [TestClass]
      public class GreeterTests
      {
         [TestMethod]
         public void SayHello_ReturnsHelloWorld()
         {
               // Arrange
               var greeter = new Greeter();

               // Act
               var result = greeter.SayHello();

               // Assert
               Assert.AreEqual("Hello, World!", result);
         }
      }
   }
   ```

   In this test, we're asserting that a method `SayHello` from a class `Greeter` should return "Hello, World!".

3. **Run Your Test**: At this point, your test will fail because we haven't implemented the `Greeter` class and the `SayHello` method yet. This is part of the TDD process: write your test first, and then make it pass. Run the test with:

   ```
   dotnet test
   ```

   You should see a failure indicating that `Greeter` does not exist.

   ```
   GreeterTests.cs(12,34): error CS0246: The type or namespace name 'HelloWorld' could not be found (are you missing a using directive or an assembly reference?) [/Users/jeromeolverasanchez/workspace/learn-dotnet-with-tests/HelloWorld/GreeterTests/GreeterTests.csproj]
   ```

## Writing the Code to Pass the Test

Now, let's write the minimal amount of code to make our test pass.

1. **Create the Source Project**: In the root directory of your solution (not inside the test project folder), create a new class library where our `Greeter` class will reside:

2. **Implement the Greeter Class**: You already have a HelloWorld project from the previous chapter. Navigate to your `HelloWorld` project and add a new class `Greeter.cs` with the following implementation:

   ```csharp
   namespace HelloWorld
   {
       public class Greeter
       {
           public string SayHello()
           {
               return "Hello, World!";
           }
       }
   }
   ```

3. **Run Your Test Again**: Now that we've implemented the required class and method, go back to your test project directory and run the tests again:

   ```
   dotnet test
   ```

   This time, your test should pass, indicating that your code correctly returns "Hello, World!".

   ```
   Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 9 ms - /Users/jeromeolverasanchez/workspace/learn-dotnet-with-tests/HelloWorld/HelloWorldTests/bin/Debug/net6.0/HelloWorldTests.dll (net6.0)
   ```

## Conclusion

Congratulations! You've just written your first piece of .NET code and test using the TDD approach. This simple exercise introduced you to the cycle of TDD: write a failing test, write code to make the test pass, and then refactor if necessary. As you progress, we'll delve into more complex scenarios and explore the powerful features of .NET and MSTest for building robust applications.

In the next chapters, we'll explore more aspects of .NET development and testing, building upon the foundations we've laid here.

Happy coding!
