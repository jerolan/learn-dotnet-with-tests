# Hello, .NET World

Welcome to your first foray into .NET development! In this chapter, we'll start with the fundamentals: writing a simple program that prints "Hello, World" to the console. Along the way, you'll learn about variables, constants, control flow statements, and, most importantly, how to write your first test using MSTest.

## Declaring Variables and Constants

In .NET, variables and constants hold data that your program can manipulate. Variables are mutable, while constants are immutable once defined.

### Variables

To declare a variable, you specify the type, followed by the name of the variable, and optionally initialize it with a value.

```csharp
string greeting = "Hello";
int number = 5;
```

### Constants

Constants are declared similarly to variables, but with the `const` keyword. Once assigned, their value cannot change.

```csharp
const double Pi = 3.14159;
```

## Control Flow: If/Else Statements and Switch

Control flow statements allow you to branch your code execution based on conditions.

### If/Else Statements

If/Else statements execute different code blocks based on a condition.

```csharp
if (number > 0) {
    Console.WriteLine("The number is positive.");
} else {
    Console.WriteLine("The number is non-positive.");
}
```

### Switch

The switch statement simplifies multiple `if` checks for the same variable, allowing for cleaner code.

```csharp
switch (greeting) {
    case "Hello":
        Console.WriteLine("English");
        break;
    case "Hola":
        Console.WriteLine("Spanish");
        break;
    default:
        Console.WriteLine("Unknown language");
        break;
}
```

## Writing Your First Program

Let's write a simple program that prints "Hello, World" to the console.

1. **Create a Project directory**: Using your explorer or terminal, create a new directory for your .NET project. For example, you can create a directory named "HelloWorld".

2. **Create a Solution File**: Open a terminal or command prompt and run:

   ```
   dotnet new sln -n HelloWorld
   ```

   This command creates a new solution file named "HelloWorld".

3. **Create a New Project**: Open a terminal or command prompt and run:

   ```
   dotnet new console -n HelloWorld
   ```

   This command creates a new console application named "HelloWorld".

4. **Add the New Project in the Solution**: To add the new project to the solution, run:

   ```
   dotnet sln add HelloWorld/HelloWorld.csproj
   ```

   This command adds the "HelloWorld" project to the "HelloWorld" solution.

5. **Edit Program.cs**: Open the `Program.cs` file in your project and replace its contents with the following:

   ```csharp
   using System;

   namespace HelloWorld {
       class Program {
           static void Main(string[] args) {
               Console.WriteLine(CreateGreeting("World"));
           }

           public static string CreateGreeting(string name) {
               return $"Hello, {name}";
           }
       }
   }
   ```

6. **Add the MSTest and Test SDK**: To add MSTest support, run:

   ```
   dotnet new mstest -n HelloWorldTests
   ```

   These commands add the necessary MSTest framework and adapter to your project and create a test project named "HelloWorldTests".

7. **Reference the Project in the Test Project**: To reference the main project in the test project, run:

   ```
   dotnet add HelloWorldTests/HelloWorldTests.csproj reference HelloWorld/HelloWorld.csproj
   ```

8. **Add the Test Project in the Solution**: To add the test project to the solution, run:

   ```
   dotnet sln add HelloWorldTests/HelloWorldTests.csproj
   ```

9. **Running Your Tests**: To ensure everything is set up correctly, navigate to your test project directory and run:

   ```
   dotnet test
   ```

   This command builds the test project and runs any tests it finds. Initially, there might not be any tests to run, but you're now set up to start TDD with .NET and MSTest!

10. **Create a New Console Application**: If you haven't already, create a new console application named "HelloWorld".

    ```bash
    dotnet new console -n HelloWorld
    ```

11. **Edit Program.cs**: Open the `Program.cs` file in your project and replace its contents with the following:

    ```csharp
    using System;

    namespace HelloWorld {
        class Program {
            static void Main(string[] args) {
                Console.WriteLine(CreateGreeting("World"));
            }

            public static string CreateGreeting(string name) {
                return $"Hello, {name}";
            }
        }
    }
    ```

12. **Run Your Program**: Navigate to your project directory in the terminal and run:

    ```bash
    dotnet run
    ```

    You should see "Hello, World" printed to the console.

## Writing Your First Test

Now, let's write a test for our `CreateGreeting` method using MSTest.

1. **Create a Test Project**: If you haven't set up your test project, refer back to the setup chapter to add MSTest to your solution.

2. **Add a Test Class**: In your test project, create a new file named `HelloWorldTests.cs` and add the following code:

   ```csharp
   using Microsoft.VisualStudio.TestTools.UnitTesting;
   using HelloWorld;

   [TestClass]
   public class HelloWorldTests {
       [TestMethod]
       public void TestCreateGreeting() {
           var result = Program.CreateGreeting("World");
           Assert.AreEqual("Hello, World", result);
       }
   }
   ```

3. **Run the Test**: Navigate to your test project directory in the terminal and run:

   ```bash
   dotnet test
   ```

   The test should pass, indicating that your `CreateGreeting` method works as expected.

## Conclusion

Congratulations! You've just written your first .NET program and test. By learning to declare variables and constants, use control flow statements, and write simple test cases, you're laying the groundwork for more complex .NET applications. Remember, TDD encourages you to think about your code's design upfront by writing tests first, leading to more reliable and maintainable software.

In the next chapters, we'll dive deeper into .NET development, exploring more complex types, data structures, and testing techniques. Stay tuned!

---

This chapter serves as an introduction to .NET programming fundamentals and TDD with MSTest, providing a practical approach to learning by doing.
