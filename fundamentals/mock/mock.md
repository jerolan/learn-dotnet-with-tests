# Mocking

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/mock)

A mock is a test double that stands in for a dependency at the edge of your code: a repository, an HTTP client, a message queue. The test controls what the double returns and can check how the code under test used it. Moq builds those doubles from an interface.

We will test a user service that reads from a repository. The finished solution is in `fundamentals/mock`. If you are building it from scratch, use the same layout as the previous chapters and add Moq to the test project:

```sh
dotnet add tests/Mocking.Tests/Mocking.Tests.csproj package Moq --version 4.20.70
```

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Mocking.Tests;

[TestClass]
public sealed class UserServiceTests
{
    [TestMethod]
    public void GetName_ReturnsTheNameFromTheRepository()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.Find(1)).Returns(new User("Ada"));
        var service = new UserService(repository.Object);

        var name = service.GetName(1);

        Assert.AreEqual("Ada", name);
        repository.Verify(r => r.Find(1), Times.Once);
    }
}
```

There are two assertions in this test. `Assert.AreEqual` checks the result; `Verify` checks that the service asked the repository for the right id exactly once. The second one is the interaction test.

## Try to run the test

The compiler does not know `IUserRepository`, `User` or `UserService` yet. Create the production code with a placeholder:

```csharp
namespace Mocking;

public sealed record User(string Name);

public interface IUserRepository
{
    User? Find(int id);
}

public sealed class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public string GetName(int id)
    {
        return string.Empty;
    }
}
```

The test compiles and fails with `Expected:<Ada>. Actual:<>`.

## Write enough code to make it pass

```csharp
public string GetName(int id)
{
    var user = _repository.Find(id);

    return user?.Name ?? "Unknown";
}
```

Green, including the `Verify`. The service depends on the interface; the test supplies a mock that answers `Find(1)` with a user.

## How Moq works

`new Mock<IUserRepository>()` creates a proxy that implements the interface. Nothing is hard-coded to a database.

`repository.Setup(r => r.Find(1))` describes the call to expect. `Returns(new User("Ada"))` is the answer for that call. `repository.Object` is the proxy instance you pass to the service, typed as `IUserRepository`.

`Verify(r => r.Find(1), Times.Once)` asserts that the call happened once. If the service passed `2` or called twice, the test fails. That is useful when the interaction is part of the contract, not when it is an implementation detail.

By default, a Moq mock is loose: a call that was not set up returns a default value (null, zero, an empty sequence). `new Mock<IUserRepository>(MockBehavior.Strict)` makes an unconfigured call throw instead. Strict mocks are noisier but catch accidental calls.

## Repeat for new requirements

### An unknown user

Write the test:

```csharp
[TestMethod]
public void GetName_UnknownUser_ReturnsUnknown()
{
    var repository = new Mock<IUserRepository>();
    repository.Setup(r => r.Find(99)).Returns((User?)null);
    var service = new UserService(repository.Object);

    var name = service.GetName(99);

    Assert.AreEqual("Unknown", name);
}
```

It passes with the current implementation, because `user?.Name ?? "Unknown"` handles the null. The test pins the fallback: if someone changes the method to `return user.Name`, the test fails with a `NullReferenceException` instead of shipping a crash.

The cast `(User?)null` tells Moq which overload of `Returns` you mean. Without it, the compiler cannot infer the nullable type of the return value.

## Refactor

There is no production code to change. The design decision is where the mock belongs.

Mock the boundary you own: `IUserRepository` is our interface, and a real implementation would hit a database. The mock replaces the database, not the business logic. If a test mocks an internal collaborator that has no I/O, a hand-written fake is often shorter and easier to read; the dependency injection chapter used one.

Do not mock types you do not own, such as `HttpClient` or a third-party SDK. Wrap them in your own interface, then mock the wrapper. Otherwise the test is coupled to an API you cannot control.

Verify the interactions that matter. "We asked for user 1 once" is a contract. "We called `Find` before `Name` was read" is usually implementation detail; asserting it makes every refactor break the test.

## Wrapping up

What we have covered:

- Creating a mock from an interface with `new Mock<T>()`.
- Setting up a return value with `Setup` and `Returns`.
- Passing `mock.Object` as a constructor dependency.
- Verifying an interaction with `Verify` and `Times.Once`.
- Loose versus strict mock behavior.
- Choosing a mock at an I/O boundary and a fake for an internal collaborator.

The chapter follows the [Mocking chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/mocking): the test replaces a slow or external collaborator with a controlled double. .NET adds the interface proxy and the verification API from Moq.

Next: [Structs and Records](../records/records.md).

