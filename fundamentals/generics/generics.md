# Generics

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/generics)

Adding a product to a repository and looking it up by id should return the same product. When we add customers later, the repository should work for both types without accepting unrelated objects.

The first call cannot compile because `ProductRepository` does not exist:

```csharp
var repository = new ProductRepository();
repository.Add(new Product(1, "Keyboard"));
```

Running `dotnet test` reports `error CS0246: The type or namespace name 'ProductRepository' could not be found`. We will start with products, add another entity type, then refactor the repeated algorithm into a generic repository. The finished solution is in `fundamentals/generics`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Generics`, an MSTest project in `tests/Generics.Tests`, a solution and a project reference.

## How it works

A repository stores entities by id. A `Dictionary<int, Product>` can find a product by its `Id`. After the tests show that products and customers need the same operations, a type parameter can let one repository implementation handle both while keeping the entity type visible to the compiler.

## How to test

The repository keeps its data in memory. A unit test can add a product, retrieve it by id and compare the result directly. Later tests will check a missing id, the full collection and the same behavior for a customer.

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
        var repository = new ProductRepository();
        var product = new Product(1, "Keyboard");

        repository.Add(product);

        Assert.AreEqual(product, repository.Get(1));
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'ProductRepository' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Start with one product type and a repository that compiles:

```csharp
namespace Generics;

public sealed record Product(int Id, string Name);

public sealed class ProductRepository
{
    public void Add(Product item)
    {
    }

    public Product? Get(int id)
    {
        return null;
    }
}
```

The test compiles and fails with `Expected:<Product { Id = 1, Name = Keyboard }>. Actual:<null>`.

## Write enough code to make it pass

```csharp
public sealed class ProductRepository
{
    private readonly Dictionary<int, Product> _items = new();

    public void Add(Product item)
    {
        _items[item.Id] = item;
    }

    public Product? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

Green. `ProductRepository` stores products in a dictionary keyed by `Id`.

## Refactor

The product repository has one lookup and one update. There is nothing to extract yet. Run the test again; it remains green. More requirements will show whether this algorithm is useful for another entity.

## Repeat for new requirements

### A missing id

Write the test:

```csharp
[TestMethod]
public void GetMissing_ReturnsNull()
{
    var repository = new ProductRepository();

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
    var repository = new ProductRepository();
    repository.Add(new Product(1, "Keyboard"));
    repository.Add(new Product(2, "Mouse"));

    var all = new List<Product>(repository.All);

    Assert.AreEqual(2, all.Count);
}
```

The compiler says `All` is missing. Add a read-only view:

```csharp
public IReadOnlyCollection<Product> All => _items.Values;
```

Green. `Dictionary<int, Product>.Values` implements `IReadOnlyCollection<Product>`, and the property exposes it without letting callers add or remove entries.

### A second entity type

Write the test:

```csharp
[TestMethod]
public void WorksWithAnotherEntityType()
{
    var repository = new CustomerRepository();
    repository.Add(new Customer(7, "Ada"));

    Assert.AreEqual("Ada", repository.Get(7)!.Name);
}
```

Add a customer and a repository with the same operations:

```csharp
public sealed record Customer(int Id, string Name);

public sealed class CustomerRepository
{
    private readonly Dictionary<int, Customer> _items = new();

    public IReadOnlyCollection<Customer> All => _items.Values;

    public void Add(Customer item)
    {
        _items[item.Id] = item;
    }

    public Customer? Get(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}
```

The test passes, but `ProductRepository` and `CustomerRepository` now duplicate the same lookup and update. Both tests should keep passing while we remove that duplication.

### Share one repository implementation

Update both entity types to implement the smallest shared contract:

```csharp
public interface IEntity
{
    int Id { get; }
}

public sealed record Product(int Id, string Name) : IEntity;
public sealed record Customer(int Id, string Name) : IEntity;
```

The tests now choose the entity type when they create the repository:

```csharp
var products = new Repository<Product>();
products.Add(new Product(1, "Keyboard"));
Assert.AreEqual("Keyboard", products.Get(1)!.Name);

var customers = new Repository<Customer>();
customers.Add(new Customer(7, "Ada"));
Assert.AreEqual("Ada", customers.Get(7)!.Name);
```

Replace both concrete classes with one generic implementation:

```csharp
public sealed class Repository<T> where T : class, IEntity
{
    private readonly Dictionary<int, T> _items = new();

    public IReadOnlyCollection<T> All => _items.Values;

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

All the behavior tests still pass. The type parameter removes duplicate code while keeping each repository tied to one entity type.

### Understand the generic constraint

`T` is the type parameter. `new Repository<Product>()` sets `T` to `Product`; `new Repository<Customer>()` sets it to `Customer`. The compiler checks each constructed type while both share one implementation.

The `where T : class, IEntity` clause is a constraint. `class` says `T` is a reference type, which lets `Get` return `T?` for a missing id. `IEntity` says each `T` has an `Id` property, which `Add` uses. Without that constraint, the compiler would reject `item.Id` because an arbitrary `T` has no known members.

### Keep the constraint narrow

The repository is small. Its key design choice is the constraint, which asks for `Id` and nothing else. A wider constraint, such as a base class with many members, would tie the repository to more than it needs and make it harder to reuse.

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

Next: [IEnumerable](../enumerable/enumerable.md).

