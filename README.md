# Learn .NET with Tests

Welcome to "Learn .NET with Tests," your journey into the world of test-driven development (TDD) within the .NET ecosystem. This guide is designed to help you master the fundamentals of TDD, a development approach that emphasizes writing tests before writing the code itself. By following this guide, you will learn not just how to write tests, but also how to think about software design and development in a way that improves code quality, reduces bugs, and enhances your productivity as a developer.

This guide it's absolutely inspired by the amazing [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/) by Chris James. I love the way he explains the concepts and the way he guides the reader through the process of learning TDD.

## What Is Test-Driven Development?

Test-Driven Development is a software development process where you write a test for a piece of functionality before you actually write the code to implement that functionality. The process follows a simple cycle known as Red-Green-Refactor:

- **Red**: Write a test for the next bit of functionality you want to add. Run the test, which should fail because the functionality isn't implemented yet.
- **Green**: Write the minimal amount of code necessary to make the test pass. This often means the code is not perfect.
- **Refactor**: Clean up the new code, ensuring it fits well with the existing codebase, without changing its behavior. The tests should still pass after refactoring.

This cycle encourages developers to write only the code necessary to pass tests, leading to cleaner, more efficient codebases.

## Why Learn TDD with .NET?

.NET is a versatile and powerful platform that supports multiple languages (C#, F#, VB.NET) and runs on various operating systems. It's used for developing a wide range of applications, from web and mobile to desktop and cloud-based apps. Learning TDD within the .NET ecosystem allows you to:

- **Improve Code Quality**: Tests written before the code ensure that your application works as intended and continues to work as you add features or refactor.
- **Enhance Your Design Skills**: TDD encourages you to think about the design of your application upfront, leading to more modular and flexible code.
- **Become More Efficient**: While it might seem slower at first, TDD can save time by catching bugs early and reducing the need for debugging.

## What Will You Learn?

### C# fundamentals

1. [Install Dotnet](install.md) - Set up your environment to write and run C# code, including installing the .NET SDK and choosing an IDE or editor.
2. [Hello, world](hello-world.md) - Dive into C# with your first program, exploring the basic structure of a C# application and the use of Console.WriteLine.
3. [Types and Variables](types.md) - Understand C#'s built-in types such as integers, floats, strings, and how to declare and use variables.
4. [Decimals](decimals.md) - Learn how to use decimal types for precise financial and scientific calculations.
5. [Declarations, Const and Var](declarations.md) - Explore different variable declaration methods, including the use of const for constants and var for type inference
6. [Control Flow](control-flow.md) - Learn control flow statements such as if, switch, and loops (for, while, etc.) to direct the execution flow of your programs.
7. [Exceptions](exceptions.md) - Learn exception handling for graceful error management in your applications.

### Object-Oriented Programming

8. [Classes and Objects](classes.md) - Learn the basics of defining classes and creating instances (objects) to model real-world entities and behaviors.
9. [Structs and Records](records.md) - Understand the use of structs for lightweight object-like structures and records for immutable data models.
10. [`with` Keyword](with.md) - Discover how to use the with keyword to create modified copies of records for immutable data handling.
11. [Properties and Methods](methods.md) - Dive into defining and using properties and methods to encapsulate data and behavior within classes.
12. [Inheritance and Interfaces](inheritance.md) - Explore how to use inheritance for code reuse and interfaces for designing flexible systems.
13. [composition over Inheritance](composition.md) - Learn why and how to favor composition over inheritance for better design flexibility.

### Advanced Topics

14. [Collections](collections.md) - Get to know the various collection types available in .NET for storing groups of related objects.
15. [Generics](generics.md) - Understand how to use generics to create type-safe data structures and methods.
16. [`ref` Keyword](refs.md) - Learn how to use the ref keyword for passing arguments by reference.
17. [Linq](linq.md) - Explore LINQ (Language Integrated Query) for querying collections and data sources in a declarative manner.
18. [Tasks](tasks.md) - Understand how to work with tasks for performing asynchronous operations.
19. [Async/Await](async.md) - Dive into the async/await pattern for writing clean, efficient asynchronous code.
20. [IQueryable](queriable.md) - Learn about IQueryable for efficient querying against data sources like databases.
21. [IEnumerable](enumerable.md) - Explore IEnumerable and its role in iterating over collections.
22. [Files and I/O ](io.md) - Get to grips with reading from and writing to files and performing other I/O operations.
23. [`using` Keyword](using.md) - Discover how to manage resources automatically with the using statement.
24. [Mocking](mocking.md) - Learn about mocking in unit testing to simulate behavior of real objects in tests.
25. [Dependency Injection](dependency-injection.md) - Understand dependency injection for creating loosely coupled, maintainable applications.
