# Classes and Objects

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/classes)

A class is a blueprint. An object is an instance of that blueprint with its own state. The class says which data the object holds and which operations it supports; the object holds the values.

We will build a bank account. It opens with a balance, accepts deposits and withdrawals, and refuses to go below zero. The finished solution is in `fundamentals/classes`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Classes`, an MSTest project in `tests/Classes.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Classes.Tests;

[TestClass]
public sealed class BankAccountTests
{
    [TestMethod]
    public void NewAccount_HasTheInitialBalance()
    {
        var account = new BankAccount(100m);

        Assert.AreEqual(100m, account.Balance);
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0246: The type or namespace name 'BankAccount' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Classes;

public class BankAccount
{
    public BankAccount(decimal initialBalance)
    {
    }

    public decimal Balance { get; set; }
}
```

The code compiles and the test fails with `Expected:<100>. Actual:<0>`. The constructor runs, but it ignores its parameter.

## Write enough code to make it pass

```csharp
public BankAccount(decimal initialBalance)
{
    Balance = initialBalance;
}
```

Green. `new BankAccount(100m)` creates an object and runs the constructor with `100m` as the argument. The object keeps that value in `Balance`; a second call to `new` would create a different object with its own balance.

## Refactor

The test passes, but `Balance` currently has a public setter: anyone can write `account.Balance = 1_000_000m` and skip the class entirely. The class should own its state.

Change the property to `{ get; private set; }`:

```csharp
public decimal Balance { get; private set; }
```

Run the tests. Still green, because the constructor can use the private setter. From outside the class, the property is read-only:

```csharp
account.Balance = 200m;
// error CS0272: The property or indexer 'BankAccount.Balance' cannot be used
// in this context because the set accessor is inaccessible
```

That is encapsulation in one line: callers can read the balance, and only the class can change it.

## Repeat for new requirements

### Deposit

Write the test:

```csharp
[TestMethod]
public void Deposit_AddsToTheBalance()
{
    var account = new BankAccount(100m);

    account.Deposit(50m);

    Assert.AreEqual(150m, account.Balance);
}
```

The compiler says `Deposit` does not exist. Add the method:

```csharp
public void Deposit(decimal amount)
{
    Balance += amount;
}
```

Green. `void` means the method returns nothing; its effect is the change it makes to the object. `Balance += amount` is shorthand for `Balance = Balance + amount`.

### Withdraw

Write the test:

```csharp
[TestMethod]
public void Withdraw_SubtractsFromTheBalance()
{
    var account = new BankAccount(100m);

    account.Withdraw(30m);

    Assert.AreEqual(70m, account.Balance);
}
```

Add the method:

```csharp
public void Withdraw(decimal amount)
{
    Balance -= amount;
}
```

Green. Now the object has two operations that change the same piece of state, and the property keeps callers from changing it directly.

### Do not allow an overdraft

A bank account can carry many balances, but it has an invariant: the balance never goes below zero. Write the test:

```csharp
[TestMethod]
public void Withdraw_MoreThanTheBalance_ThrowsAndKeepsTheBalance()
{
    var account = new BankAccount(100m);

    Assert.ThrowsException<InvalidOperationException>(() => account.Withdraw(150m));

    Assert.AreEqual(100m, account.Balance);
}
```

Run it. The test fails because no exception is thrown; `Withdraw` happily leaves the balance at `-50`. Add the guard before the subtraction:

```csharp
public void Withdraw(decimal amount)
{
    if (amount > Balance)
    {
        throw new InvalidOperationException("Insufficient funds.");
    }

    Balance -= amount;
}
```

Green. The second assertion matters as much as the first: the failed operation must leave the object in its previous state. Check the condition before you mutate.

## Objects are references

Write one more test:

```csharp
[TestMethod]
public void TwoVariables_CanPointToTheSameAccount()
{
    var account = new BankAccount(100m);
    var alias = account;

    alias.Deposit(50m);

    Assert.AreEqual(150m, account.Balance);
}
```

It passes without changing the production code. `BankAccount` is a reference type: `account` holds a reference to the object, and `alias = account` copies the reference, not the object. Both variables point to the same balance. A `struct` or a `record struct` would copy the value instead; the next chapter looks at that difference.

This is the same lesson as pointers in other languages, without pointer arithmetic: two names, one object, and a mutation visible through both.

## Wrapping up

What we have covered:

- A class as a blueprint and `new` as the way to create an object.
- A constructor that gives the object its initial state.
- A property with a public getter and a private setter.
- Methods that mutate state and a `void` return type.
- An invariant enforced before the mutation, so a failed operation changes nothing.
- Reference semantics: two variables can point to the same object.

The chapter follows the type-and-method progression of [Structs, Methods and Interfaces in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces), with the reference behavior from [Pointers in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors). .NET makes the reference semantics explicit with classes and value semantics explicit with structs and records.

Next: [Structs and Records](../records/records.md).

