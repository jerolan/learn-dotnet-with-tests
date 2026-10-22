# Dependency Injection

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/dependency-injection)

A class that creates its own dependencies is hard to test and hard to change. Dependency injection means the class receives what it needs from the outside, usually through its constructor. The class keeps its behavior; the caller decides which implementation to supply.

We will build a notifier that sends an alert through one or more channels. The finished solution is in `fundamentals/dependency-injection`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/DependencyInjection`, an MSTest project in `tests/DependencyInjection.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependencyInjection.Tests;

[TestClass]
public sealed class NotifierTests
{
    [TestMethod]
    public void Notify_SendsTheFormattedMessage()
    {
        var sender = new RecordingSender();
        var notifier = new Notifier(sender);

        notifier.Notify("Disk full");

        Assert.AreEqual(1, sender.Messages.Count);
        Assert.AreEqual("[Alert] Disk full", sender.Messages[0]);
    }

    private sealed class RecordingSender : IMessageSender
    {
        public List<string> Messages { get; } = new List<string>();

        public void Send(string message)
        {
            Messages.Add(message);
        }
    }
}
```

`RecordingSender` is a fake: it implements the contract and records what it receives. The test does not need an email server or a phone; it needs a sender it can inspect.

## Try to run the test

```text
error CS0246: The type or namespace name 'IMessageSender' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Create the contract and a notifier that accepts it:

```csharp
namespace DependencyInjection;

public interface IMessageSender
{
    void Send(string message);
}

public sealed class Notifier
{
    private readonly IMessageSender _sender;

    public Notifier(IMessageSender sender)
    {
        _sender = sender;
    }

    public void Notify(string message)
    {
    }
}
```

The test compiles and fails with `Expected:<1>. Actual:<0>` on the message count.

## Write enough code to make it pass

```csharp
public void Notify(string message)
{
    _sender.Send($"[Alert] {message}");
}
```

Green. `Notifier` does not know whether the sender writes to email, SMS or a log. It knows the interface and calls it. The constructor is the injection point: the test passes a fake, and production passes a real sender.

## Repeat for new requirements

### More than one channel

Write the test:

```csharp
[TestMethod]
public void Notify_SendsToEverySender()
{
    var email = new RecordingSender();
    var sms = new RecordingSender();
    var notifier = new Notifier(new IMessageSender[] { email, sms });

    notifier.Notify("Disk full");

    Assert.AreEqual("[Alert] Disk full", email.Messages[0]);
    Assert.AreEqual("[Alert] Disk full", sms.Messages[0]);
}
```

The compiler says there is no constructor that takes an array. Change the dependency to a collection:

```csharp
public sealed class Notifier
{
    private readonly IEnumerable<IMessageSender> _senders;

    public Notifier(IEnumerable<IMessageSender> senders)
    {
        _senders = senders;
    }

    public void Notify(string message)
    {
        var alert = $"[Alert] {message}";

        foreach (var sender in _senders)
        {
            sender.Send(alert);
        }
    }
}
```

The first test no longer compiles because `Notifier` now expects a collection. Update it to pass the single fake in an array:

```csharp
var notifier = new Notifier(new IMessageSender[] { sender });
```

Green. The notifier still has no idea how many channels exist or what they do. The caller decides; a test can pass two fakes, and the application can pass email and SMS.

## Refactor

The design is already small, but the important decision is where the concrete types live. `Notifier` never says `new EmailSender()`. Somewhere at the start of the application, a composition root does:

```csharp
IMessageSender[] senders =
{
    new EmailSender(),
    new SmsSender()
};

var notifier = new Notifier(senders);
```

The composition root is allowed to know the concrete classes. Everything else depends on `IMessageSender`. That keeps the notifier testable and lets you add a channel without editing it.

Constructor injection is the default because it makes the dependency mandatory and visible in the signature. A class that reaches into a static service locator hides the same dependency and makes the test setup less obvious.

The container in a larger application is a convenience for building this graph. The concept is the same: the class declares what it needs, and something outside creates it. The ASP.NET Core container will appear in Part 3; the notifier does not need it to be correct.

## Wrapping up

What we have covered:

- A dependency as a constructor parameter typed as an interface.
- A fake that records calls so the test can inspect them.
- Constructor injection of one sender and then of a collection of senders.
- The composition root as the one place that knows the concrete types.
- Constructor injection as the default over a hidden service locator.

The chapter follows the dependency injection chapter of [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/dependency-injection): the domain logic receives its side effect through an interface, and the test supplies a stand-in. .NET adds constructor injection and, later, a container.

Next: [Mocking](../mock/mock.md).

