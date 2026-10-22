# Working Without Mocks

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/working-without-mocks)

A mock records calls and lets the test verify them. A fake is a small working implementation. When the collaborator is an interface you own and the behavior is simple, the fake is often shorter, easier to read and less coupled to the shape of the calls.

We will test a todo service with an in-memory repository and no mocking library. The finished solution is in `testing/working-without-mocks`.

## How it works

`TodoService` depends on an `ITodoRepository` interface. The test supplies an in-memory implementation that stores todos in a list, so the service can be tested without a database. A mock could record calls instead, but this test cares about the todos the service produces.

## How to test

Keep the fake in the test project. It implements the repository contract with the smallest useful storage, and the test checks its state after using the service.

```csharp
private sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<Todo> _todos = new();

    public void Add(Todo todo)
    {
        _todos.Add(todo);
    }

    public IReadOnlyList<Todo> All()
    {
        return _todos;
    }
}
```

## Write the test first

```csharp
[TestMethod]
public void Add_StoresTheTodo()
{
    var repository = new InMemoryTodoRepository();
    var service = new TodoService(repository);

    service.Add(1, "Write tests");

    var todos = service.List();

    Assert.AreEqual(1, todos.Count);
    Assert.AreEqual("Write tests", todos[0].Title);
}
```

The test uses the fake as the collaborator and checks the resulting state.

## Try to run the test

```text
The compiler cannot find `Todo`, `ITodoRepository` or `TodoService` yet.
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace TodoApp;

public sealed record Todo(int Id, string Title);

public interface ITodoRepository
{
    void Add(Todo todo);

    IReadOnlyList<Todo> All();
}

public sealed class TodoService
{
    private readonly ITodoRepository _repository;

    public TodoService(ITodoRepository repository)
    {
        _repository = repository;
    }

    public void Add(int id, string title)
    {
    }

    public IReadOnlyList<Todo> List()
    {
        return Array.Empty<Todo>();
    }
}
```

The test compiles and fails with `Expected:<1>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public void Add(int id, string title)
{
    _repository.Add(new Todo(id, title));
}

public IReadOnlyList<Todo> List()
{
    return _repository.All();
}
```

Green. The fake stores the todo in a list, and `List` returns it. No mock, no setup and no verification of call order.

## Refactor

The production implementation is small, and there is no duplicated behavior to extract. Keep the test green; use a fake when the test cares about resulting state and a mock when the interaction itself is the contract.

### Choose a fake or a mock

Use a fake when:

- You own the interface and can write a small implementation.
- The test cares about the resulting state.
- The collaborator has behavior that is easy to model in memory, such as a repository, a cache or a clock.

Use a mock when:

- The interaction itself is the contract, such as "send exactly one email" or "call the payment gateway once".
- The collaborator is an external boundary with a large API you do not want to implement.
- You need to simulate a specific failure from a dependency without building a full fake.

The fake lives in the test project and can be reused by many tests. If it grows complex, that is a signal: either the interface is doing too much or the behavior belongs in a real adapter with its own tests.

Mocks and fakes answer different questions. A mock says "did you call this, with these arguments?" A fake says "given this behavior, what did the system produce?" Most tests are easier to read when they ask the second question.

## Repeat for new requirements

### Reject a blank title

Write the test:

```csharp
[TestMethod]
public void Add_WithBlankTitle_ThrowsAndStoresNothing()
{
    var repository = new InMemoryTodoRepository();
    var service = new TodoService(repository);

    Assert.ThrowsException<ArgumentException>(() => service.Add(1, " "));

    Assert.AreEqual(0, repository.All().Count);
}
```

It fails because the service stores the blank title. Add the guard before the repository call:

```csharp
if (string.IsNullOrWhiteSpace(title))
{
    throw new ArgumentException("Title is required.", nameof(title));
}

_repository.Add(new Todo(id, title));
```

Green. The second assertion is the important one: a rejected todo must not reach the repository. The fake makes that state visible without a `Verify` call.

## Wrapping up

What we have covered:

- A hand-written in-memory fake for a repository interface.
- Tests that assert resulting state instead of call choreography.
- A guard that rejects invalid input before it reaches the repository.
- When a fake is a better fit than a mock, and when the interaction really is the contract.

The chapter follows [Working without mocks in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/working-without-mocks): a small real implementation keeps the test close to the behavior. The next chapter turns the refactoring habits into a checklist.

Next: [Refactoring checklist](../refactoring-checklist/refactoring-checklist.md).

