# Inheritance and Interfaces

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/inheritance)

The area of a 3-by-4 rectangle is 12. A circle with radius 1 has area `Math.PI`. Both shapes can answer the same question, `Area()`, even though they use different formulas.

The first test cannot compile because `Rectangle` does not exist:

```csharp
Assert.AreEqual(12.0, new Rectangle(3, 4).Area());
```

Running `dotnet test` reports `error CS0246: The type or namespace name 'Rectangle' could not be found`. We will start with one shape, add a circle, then use tests to discover their shared contract. The finished solution is in `fundamentals/inheritance`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Inheritance`, an MSTest project in `tests/Inheritance.Tests`, a solution and a project reference.

## How it works

Each shape owns the data needed for its `Area()` calculation. An interface can declare that shared method without providing a formula. An abstract base class can hold state shared by the shapes while leaving each formula to the concrete class.

## How to test

Area calculations have no side effects, so the tests can create shapes and compare their results directly. The total test will call the same method through the shared interface. Since circle areas use `double`, assertions allow a small tolerance instead of requiring exact floating-point equality.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inheritance.Tests;

[TestClass]
public sealed class ShapeTests
{
    [TestMethod]
    public void Rectangle_Area_ReturnsWidthTimesHeight()
    {
        Assert.AreEqual(12.0, new Rectangle(3, 4).Area());
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'Rectangle' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Inheritance;

public sealed class Rectangle
{
    public Rectangle(double width, double height)
    {
    }

    public double Area()
    {
        return 0;
    }
}
```

The code compiles and the test fails with `Expected:<12>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public sealed class Rectangle
{
    private readonly double _width;
    private readonly double _height;

    public Rectangle(double width, double height)
    {
        _width = width;
        _height = height;
    }

    public double Area()
    {
        return _width * _height;
    }
}
```

Green.

## Refactor

The rectangle's area is one multiplication, so there is nothing to extract yet. Run the test again; it remains green. The next requirement will add another formula and show what behavior the shapes share.

## Repeat for new requirements

### A second shape

Write the test:

```csharp
[TestMethod]
public void Circle_Area_UsesPi()
{
    Assert.AreEqual(Math.PI, new Circle(1).Area(), 0.0001);
}
```

Add the class with a placeholder area, watch it fail with `Expected:<3.14159...>. Actual:<0>`, and then implement it:

```csharp
public sealed class Circle
{
    private readonly double _radius;

    public Circle(double radius)
    {
        _radius = radius;
    }

    public double Area()
    {
        return Math.PI * _radius * _radius;
    }
}
```

Green. The third argument to `Assert.AreEqual` is a tolerance. Comparing floating-point values exactly is a bad idea, so the test accepts a difference of `0.0001`.

### Find the contract

`Rectangle` and `Circle` have the same method signature and no shared state. That is an interface: a promise that a type can do something, without a base class.

Extract it:

```csharp
public interface IShape
{
    double Area();
}
```

and declare `Rectangle : IShape` and `Circle : IShape`. Run the tests. Green, with no behavior change. The interface is now the type a caller can use when it only needs an area.

### Give every shape a name

Write the test:

```csharp
[TestMethod]
public void Shapes_HaveNames()
{
    Assert.AreEqual("Rectangle", new Rectangle(1, 1).Name);
    Assert.AreEqual("Circle", new Circle(1).Name);
}
```

The compiler says `Rectangle` has no `Name`. Add the property to the contract:

```csharp
public interface IShape
{
    string Name { get; }

    double Area();
}
```

Now the compiler says both classes fail to implement `IShape.Name`. A name is shared by every shape, so this is the point where a base class earns its place:

```csharp
public abstract class Shape : IShape
{
    protected Shape(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract double Area();
}
```

`Shape` implements the interface and provides `Name`. `Area` stays abstract, because the base class does not know the formula. Make the concrete classes inherit from it:

```csharp
public sealed class Rectangle : Shape
{
    private readonly double _width;
    private readonly double _height;

    public Rectangle(double width, double height)
        : base("Rectangle")
    {
        _width = width;
        _height = height;
    }

    public override double Area()
    {
        return _width * _height;
    }
}

public sealed class Circle : Shape
{
    private readonly double _radius;

    public Circle(double radius)
        : base("Circle")
    {
        _radius = radius;
    }

    public override double Area()
    {
        return Math.PI * _radius * _radius;
    }
}
```

Green. A few things are new here:

- `abstract` on `Shape` means you cannot write `new Shape("x")`. The class exists to be inherited.
- The `protected` constructor is callable from a derived class, not from outside.
- `: base("Rectangle")` runs the base constructor and passes the name.
- `override` replaces the abstract `Area`. The compiler requires it.

The name lives in one place, and both shapes inherit it. The area stays in each shape because only the shape knows its formula.

### Polymorphism

Write the test:

```csharp
[TestMethod]
public void Total_SumsEveryShapeInTheList()
{
    var shapes = new IShape[] { new Rectangle(3, 4), new Circle(1) };

    Assert.AreEqual(12 + Math.PI, AreaCalculator.Total(shapes), 0.0001);
}
```

The compiler says `AreaCalculator` does not exist. Add it:

```csharp
public static class AreaCalculator
{
    public static double Total(IEnumerable<IShape> shapes)
    {
        var total = 0.0;

        foreach (var shape in shapes)
        {
            total += shape.Area();
        }

        return total;
    }
}
```

Green. `AreaCalculator` never mentions `Rectangle` or `Circle`. It knows only `IShape`, calls `Area()`, and the runtime picks the right implementation for each object. That is polymorphism: one call site, many behaviors.

The same idea works with the base class: a `Shape[]` can hold both shapes and call the inherited `Name` property.

### Refactor the hierarchy

The hierarchy is now small and each piece has one reason to exist: the interface names the contract, the abstract class holds shared state, and the concrete classes provide the formulas.

A class can implement many interfaces but inherit from only one base class. When a type needs to be used through more than one contract, interfaces are the way to do it. When several types share state or a template of behavior, a base class is the way to avoid repeating it.

`sealed` on `Rectangle` and `Circle` says nobody is expected to derive from them. If a shape later needs a subclass, remove `sealed` deliberately.

## Wrapping up

What we have covered:

- Two concrete shapes and their area formulas.
- An interface as a contract with no implementation.
- An abstract base class that implements part of the contract and leaves the rest abstract.
- `protected` constructors, `base(...)` calls and `override`.
- Polymorphism through an interface, with one loop calling many implementations.
- The trade-off between a base class and an interface.

The chapter follows the interface section of [Structs, Methods and Interfaces in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): define the behavior you need, then let each type provide it. .NET adds class inheritance on top of the interface contract.

Next: [Composition over Inheritance](../composition/composition.md).

