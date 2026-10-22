# Generics

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/generics)

A generic type or method takes a type as a parameter. You write the algorithm once, the compiler checks it against every type you use it with, and you keep compile-time safety instead of casting to `object`.

We will build a repository that stores entities by id and works for any entity type. The finished solution is in `fundamentals/generics`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Generics`, an MSTest project in `tests/Generics.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Generics.Tests;

[TestClass]
public sealed class RepositoryTests
{
    [TestMethod]
    public void AddThenGet_ReturnsTheItem()
    {
        var repository = new Repository<Product>();
        var product = new Product(1, "Keyboard");

        repository.Add(product);

        Assert.AreEqual(product, repository.Get(1));
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'Repository' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Start with the contract the repository needs from an item, one entity, and a generic repository that compiles:

```csharp
namespace Generics;

public interface IEntity
{
    int Id { get; }
}

public sealed record Product(int Id, string Name) : IEntity;

public sealed class Repository<T> where T : class, IEntity
{
    public void Add(T item)
    {
    }

    public T? Get(int id)
    {
        return null;
    }
}
```

The test compiles and fails with `Expected:<Product { Id = 1, Name = Keyboard }>. Actual:<null>`.

## Write enough code to make it pass

```csharp
public sealed class Repository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public void Add(T item)
    {
        _items[item.Id] = item;
    }

    public T? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

Green. `Repository<T>` stores values of `T` in a dictionary keyed by `int`. The dictionary is generic too: `Dictionary<int, T>`.

## How it works

`T` is the type parameter. `new Repository<Product>()` closes it with `Product`; `new Repository<Customer>()` closes it with `Customer`. Each closed type is a distinct type to the compiler, and both share the same source code.

The `where T : class, IEntity` clause is a constraint. `class` says `T` is a reference type, which lets the method return `T?` as null. `IEntity` says every `T` has an `Id` property, which is what `Add` uses. Without the constraint, the compiler would reject `item.Id`: a random `T` has no members.

The repository never mentions `Product` or `Customer`. It knows only the interface it requires. That is the point of the constraint: it describes the minimum the algorithm needs.

## Repeat for new requirements

### A missing id

Write the test:

```csharp
[TestMethod]
public void GetMissing_ReturnsNull()
{
    var repository = new Repository<Product>();

    Assert.IsNull(repository.Get(99));
}
```

It passes with the current implementation because `TryGetValue` returns `false` and the method returns null. The test pins that contract: a missing id is not an exception here.

### Every item

Write the test:

```csharp
[TestMethod]
public void All_ReturnsEveryItem()
{
    var repository = new Repository<Product>();
    repository.Add(new Product(1, "Keyboard"));
    repository.Add(new Product(2, "Mouse"));

    var all = new List<Product>(repository.All);

    Assert.AreEqual(2, all.Count);
}
```

The compiler says `All` is missing. Add a read-only view:

```csharp
public IReadOnlyCollection<T> All => _items.Values;
```

Green. `Dictionary<int, T>.Values` implements `IReadOnlyCollection<T>`, and the property exposes it without letting callers add or remove entries.

### A second entity type

Write the test:

```csharp
[TestMethod]
public void WorksWithAnotherEntityType()
{
    var repository = new Repository<Customer>();
    repository.Add(new Customer(7, "Ada"));

    Assert.AreEqual("Ada", repository.Get(7)!.Name);
}
```

Add the entity:

```csharp
public sealed record Customer(int Id, string Name) : IEntity;
```

Green without touching `Repository<T>`. That is the payoff: one implementation, two entity types, and the compiler checked both.

## Refactor

The repository is small. The design choices are the constraint and the dictionary.

The constraint is deliberately narrow. It asks for `Id` and nothing else. A wider constraint, such as a base class with many members, would tie the repository to more than it needs and make it harder to reuse.

Generics are not the same as `object`. A `List<object>` would accept anything, force casts on the way out, and box value types. `List<T>` keeps the element type in the signature and lets the compiler reject a wrong assignment.

If the repository later needs to be replaced in tests, extract `IRepository<T>` and inject it. The generic type parameter belongs on the interface too:

```csharp
public interface IRepository<T> where T : class, IEntity
{
    void Add(T item);

    T? Get(int id);
}
```

## Wrapping up

What we have covered:

- A generic class with a type parameter, `Repository<T>`.
- A constraint, `where T : class, IEntity`, that documents what the code needs.
- A generic dictionary, `Dictionary<int, T>`.
- A nullable return, `T?`, and a missing-id contract.
- A read-only collection view.
- The same implementation used with two entity types.

The chapter follows the [Generics chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/generics): a function or type that works across element types while the compiler keeps the types honest. .NET adds constraints and variance on top of the same idea.

Next: [The ref keyword](../ref/ref.md).

