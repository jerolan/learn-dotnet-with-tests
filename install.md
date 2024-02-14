# Setting Up Your .NET Environment

Welcome to the first step in your journey to mastering .NET development through Test-Driven Development (TDD). Before diving into writing code and tests, it's crucial to set up your development environment. This chapter will guide you through installing the necessary tools for .NET development, focusing on MSTest for unit testing.

## What You Need

To develop .NET applications, you'll need the following:

- **.NET SDK**: The Software Development Kit (SDK) includes everything you need to develop and run .NET applications.
- **A Code Editor**: While you can use any text editor, Visual Studio Code (VS Code) is highly recommended due to its excellent .NET Core support and integrated terminal.
- **The MSTest Framework**: For writing and running tests, we'll use MSTest, a flexible, powerful testing framework.

## Installing the .NET SDK

1. **Download the SDK**: Visit the official [.NET download page](https://dotnet.microsoft.com/download) and download the .NET SDK for your operating system.
2. **Installation**: Follow the installation instructions for your platform (Windows, Linux, macOS). After installation, you can verify it by opening a terminal and running:

   ```
   dotnet --version
   ```

   This command should display the installed .NET SDK version.

## Setting Up Visual Studio Code (VS Code)

VS Code is a lightweight, powerful code editor by Microsoft. It's available for Windows, Linux, and macOS.

1. **Download and Install**: Download VS Code from the [official website](https://code.visualstudio.com/) and follow the installation instructions for your OS.

2. **Install the C# Extension**: Open VS Code, go to the Extensions view by clicking on the square icon on the sidebar, or pressing `Ctrl+Shift+X`. Search for "C#" and install the extension by Microsoft. This extension adds support for .NET development, including debugging and IntelliSense.

## Creating Your First .NET Project with MSTest

Now that your environment is set up, let's create a new .NET project and add MSTest for unit testing.

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

5. **Add the MSTest and Test SDK**: To add MSTest support, run:

   ```
   dotnet new mstest -n HelloWorldTests
   ```

   These commands add the necessary MSTest framework and adapter to your project and create a test project named "HelloWorldTests".

6. **Add the Test Project in the Solution**: To add the test project to the solution, run:

   ```
   dotnet sln add HelloWorldTests/HelloWorldTests.csproj
   ```

7. **Running Your Tests**: To ensure everything is set up correctly, navigate to your test project directory and run:

   ```
   dotnet test
   ```

   This command builds the test project and runs any tests it finds. Initially, there might not be any tests to run, but you're now set up to start TDD with .NET and MSTest!

## Conclusion

You're now ready to start your TDD journey with .NET and MSTest. In the next chapters, we'll dive into writing actual tests and code, following the TDD methodology. Remember, the key to TDD is to write your tests before your production code, ensuring your software is designed to be testable and robust from the start.

Happy coding!

---

This template follows the instructional flow similar to "learn-go-with-tests" but is tailored for .NET learners, focusing on setting up the environment, installing necessary tools, and preparing for TDD with MSTest.
