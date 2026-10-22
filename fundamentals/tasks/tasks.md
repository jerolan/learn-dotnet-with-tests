# Tasks

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/tasks)

A `Task` is a handle to work that may finish later. `Task<T>` adds a result. You can await one task, wait for all of them, or take the first one that finishes. The tests can control when a task completes, so none of this needs a real delay.

We will build two combinators: sum the results of several tasks, and return the first result. The finished solution is in `fundamentals/tasks`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Tasks`, an MSTest project in `tests/Tasks.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tasks.Tests;

[TestClass]
public sealed class TaskCombinatorsTests
{
    [TestMethod]
    public async Task SumAsync_AddsCompletedTasks()
    {
        var sum = await TaskCombinators.SumAsync(
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3));

        Assert.AreEqual(6, sum);
    }
}
```

The test method returns `Task`, not `void`. MSTest awaits it, so a failure inside the async test is reported like any other failure.

## Try to run the test

```text
error CS0246: The type or namespace name 'TaskCombinators' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Tasks;

public static class TaskCombinators
{
    public static Task<int> SumAsync(params Task<int>[] tasks)
    {
        return Task.FromResult(0);
    }
}
```

The test compiles and fails with `Expected:<6>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public static async Task<int> SumAsync(params Task<int>[] tasks)
{
    var values = await Task.WhenAll(tasks);

    var sum = 0;

    foreach (var value in values)
    {
        sum += value;
    }

    return sum;
}
```

Green. `Task.WhenAll(tasks)` returns a task that completes when every input completes, with an `int[]` of their results. `await` unwraps that array.

The `async` keyword lets the method use `await` and return `int` while the signature says `Task<int>`. The caller sees a task; the result appears when the task completes.

## How it works

A `Task` has a status: waiting, running, completed, faulted or canceled. `Task.FromResult(1)` creates an already-completed task. `Task.WhenAll` creates a new task that completes when all its inputs do. `Task.WhenAny` creates one that completes when the first input does.

`await` does not block the thread. It registers a continuation and returns control to the caller until the task completes. In an MSTest method, the runner waits for the returned `Task` before deciding the test result.

## Repeat for new requirements

### Wait for every task

Write the test:

```csharp
[TestMethod]
public async Task SumAsync_WaitsForEveryTask()
{
    var first = new TaskCompletionSource<int>();
    var second = new TaskCompletionSource<int>();

    var sumTask = TaskCombinators.SumAsync(first.Task, second.Task);

    Assert.IsFalse(sumTask.IsCompleted);

    first.SetResult(1);

    Assert.IsFalse(sumTask.IsCompleted);

    second.SetResult(2);

    Assert.AreEqual(3, await sumTask);
}
```

It passes. `TaskCompletionSource<int>` gives the test a task it can complete by hand. After the first result, the sum task is still incomplete; after the second, it finishes. No `Task.Delay` and no timing assumptions.

That is the test you want for "wait for all": it fails if the implementation returns after the first task.

### The first task to finish

Write the test:

```csharp
[TestMethod]
public async Task FirstAsync_ReturnsTheFirstCompletedTask()
{
    var slow = new TaskCompletionSource<int>();
    var fast = new TaskCompletionSource<int>();

    var firstTask = TaskCombinators.FirstAsync(slow.Task, fast.Task);

    fast.SetResult(20);

    Assert.AreEqual(20, await firstTask);
}
```

Add the method:

```csharp
public static async Task<int> FirstAsync(params Task<int>[] tasks)
{
    var completed = await Task.WhenAny(tasks);

    return await completed;
}
```

Green. `Task.WhenAny` returns the task that finished first; the second `await` unwraps its result. The slow task is still pending and is not awaited, so its result is ignored.

### No tasks

Write the boundary:

```csharp
[TestMethod]
public async Task SumAsync_WithNoTasks_ReturnsZero()
{
    Assert.AreEqual(0, await TaskCombinators.SumAsync());
}
```

It passes. `Task.WhenAll` over an empty array returns an empty array, and the sum starts at zero.

## Refactor

The combinators are short; the habits around them matter.

Do not block on a task with `.Result` or `.Wait()`. In a UI application or an ASP.NET request, blocking a thread while it waits for work that needs that same thread can deadlock. It also wraps failures in an `AggregateException` instead of letting `await` surface the original exception.

Use `async Task` for asynchronous methods. `async void` is only for event handlers: the caller cannot await it, and an exception thrown inside it is not observable in the normal way.

`Task.Run` is for CPU-bound work that you want to move off the current thread. I/O work such as a database call or an HTTP request is already asynchronous; wrapping it in `Task.Run` only adds a thread hop.

`Task.WhenAll` collects failures from several tasks. Awaiting the combined task throws one exception, and the others are available through the task's `Exception` property. The next chapter handles exceptions and cancellation in more detail.

## Wrapping up

What we have covered:

- `Task` and `Task<T>` as handles to work in progress.
- `async` test methods and `await`.
- `Task.WhenAll` and its result array.
- `Task.WhenAny` for the first completed task.
- `TaskCompletionSource<T>` for deterministic tests.
- The empty-input boundary.
- Why to avoid `.Result`, `.Wait()`, `async void` and unnecessary `Task.Run`.

The chapter follows the concurrency goals of the [Concurrency chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/concurrency): several pieces of work run independently, and the caller decides how to combine them. .NET expresses the combination with `Task.WhenAll` and `Task.WhenAny`.

Next: [Async/Await](../async/async.md).

