# Learn .NET with Tests

Welcome to "Learn .NET with Tests," your journey into the world of test-driven development (TDD) within the .NET ecosystem. This guide is designed to help you master the fundamentals of TDD, a development approach that emphasizes writing tests before writing the code itself. By following this guide, you will learn not just how to write tests, but also how to think about software design and development in a way that improves code quality, reduces bugs, and enhances your productivity as a developer.

This guide is inspired by the amazing [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/) by Chris James. I love the way he explains the concepts and the way he guides the reader through the process of learning TDD.

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

1. 🟢 [Install .NET](fundamentals/install/install.md) ([ES](fundamentals/install/install.es.md)) - Set up your environment to write and run C# code, including installing the .NET SDK and choosing an IDE or editor.
2. 🟢 [Hello, World](fundamentals/hello-world/hello-world.md) ([ES](fundamentals/hello-world/hello-world.es.md)) - Write your first program, separate the greeting from the console, and grow it test first.
3. 🟢 [Types and Variables](fundamentals/types/types.md) ([ES](fundamentals/types/types.es.md)) - Work with integers, doubles and booleans through failing tests and a table of cases.
4. 🟢 [Decimals](fundamentals/decimal/decimal.md) ([ES](fundamentals/decimal/decimal.es.md)) - See why money needs `decimal` and build a `Money` value with an explicit rounding policy.
5. 🟢 [Declarations, Const and Var](fundamentals/declarations/declarations.md) ([ES](fundamentals/declarations/declarations.es.md)) - Learn when a value is a variable, a compile-time constant or a readonly field.
6. 🟢 [Control Flow](fundamentals/control-flow/control-flow.md) ([ES](fundamentals/control-flow/control-flow.es.md)) - Use `for`, `while`, `if` and a switch expression to drive behavior.
7. 🟢 [Exceptions](fundamentals/exceptions/exceptions.md) ([ES](fundamentals/exceptions/exceptions.es.md)) - Make the error path part of the contract with a domain exception.

### Object-Oriented Programming

8. 🟢 [Properties and Methods](fundamentals/methods/methods.md) ([ES](fundamentals/methods/methods.es.md)) - Define methods, properties, optional parameters and named arguments.
9. 🟢 [Classes and Objects](fundamentals/classes/classes.md) ([ES](fundamentals/classes/classes.es.md)) - Model state with a constructor, a private setter and an invariant.
10. 🟢 [Structs and Records](fundamentals/records/records.md) ([ES](fundamentals/records/records.es.md)) - Compare value copying in structs with value equality in records.
11. 🟢 [`with` Keyword](fundamentals/with/with.md) ([ES](fundamentals/with/with.es.md)) - Create changed copies of records without mutating the original.
12. 🟢 [Inheritance and Interfaces](fundamentals/inheritance/inheritance.md) ([ES](fundamentals/inheritance/inheritance.es.md)) - Share state with a base class and behavior with an interface.
13. 🟢 [Composition over Inheritance](fundamentals/composition/composition.md) ([ES](fundamentals/composition/composition.es.md)) - Assemble behavior from small parts instead of multiplying subclasses.

### Advanced Topics

14. 🟢 [Dependency Injection](fundamentals/dependency-injection/dependency-injection.md) ([ES](fundamentals/dependency-injection/dependency-injection.es.md)) - Receive dependencies through the constructor and keep the composition root at the edge.
15. 🟢 [Mocking](fundamentals/mock/mock.md) ([ES](fundamentals/mock/mock.es.md)) - Use Moq at an I/O boundary and verify the interactions that are part of the contract.
16. 🟢 [Collections](fundamentals/collections/collections.md) ([ES](fundamentals/collections/collections.es.md)) - Choose between arrays, lists, dictionaries and sets with a word counter.
17. 🟢 [Generics](fundamentals/generics/generics.md) ([ES](fundamentals/generics/generics.es.md)) - Write a repository once and use it with several entity types.
18. 🟢 [IEnumerable](fundamentals/enumerable/enumerable.md) ([ES](fundamentals/enumerable/enumerable.es.md)) - Build lazy sequences with `yield` and understand deferred execution.
19. 🟢 [LINQ](fundamentals/linq/linq.md) ([ES](fundamentals/linq/linq.es.md)) - Filter, group, order and aggregate sequences with method syntax.
20. 🟢 [IQueryable](fundamentals/queriable/queriable.md) ([ES](fundamentals/queriable/queriable.es.md)) - See how a query becomes an expression tree and why the provider matters.
21. 🟢 [`ref` Keyword](fundamentals/ref/ref.md) ([ES](fundamentals/ref/ref.es.md)) - Pass references, return references and pass readonly structs with `in`.
22. 🟢 [Tasks](fundamentals/tasks/tasks.md) ([ES](fundamentals/tasks/tasks.es.md)) - Combine asynchronous work with `WhenAll` and `WhenAny`.
23. 🟢 [Async/Await](fundamentals/async/async.md) ([ES](fundamentals/async/async.es.md)) - Write asynchronous methods, propagate exceptions and honor cancellation.
24. 🟢 [Files and I/O](fundamentals/io/io.md) ([ES](fundamentals/io/io.es.md)) - Read and write through `TextReader` and `TextWriter` without touching the disk in tests.
25. 🟢 [`using` Keyword](fundamentals/using/using.md) ([ES](fundamentals/using/using.es.md)) - Release resources deterministically with `IDisposable` and `using`.

## Part 2: Testing fundamentals

These chapters turn the testing habits from Part 1 on the test suite itself. They are the bridge to the application in Part 3.

1. 🟢 [Acceptance Tests](testing/acceptance-tests/acceptance-tests.md) ([ES](testing/acceptance-tests/acceptance-tests.es.md)) - Test a feature through HTTP with WebApplicationFactory and keep the test at the user-visible boundary.
2. 🟢 [Scaling Acceptance Tests](testing/scaling-acceptance-tests/scaling-acceptance-tests.md) ([ES](testing/scaling-acceptance-tests/scaling-acceptance-tests.es.md)) - Keep many scenarios readable with a test driver, builders and isolated factories.
3. 🟢 [Working Without Mocks](testing/working-without-mocks/working-without-mocks.md) ([ES](testing/working-without-mocks/working-without-mocks.es.md)) - Replace interaction mocks with a small in-memory fake when the collaborator is yours.
4. 🟢 [Refactoring Checklist](testing/refactoring-checklist/refactoring-checklist.md) ([ES](testing/refactoring-checklist/refactoring-checklist.es.md)) - Change structure with the tests green and a repeatable list of checks.
