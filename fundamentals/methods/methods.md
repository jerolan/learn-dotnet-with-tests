# Properties and Methods

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/methods)

A 2 kg shipment at a rate of 5m per kilogram should cost 10m. Express delivery adds a 20m surcharge. We need a calculator that can express both rules and let the caller set the rate.

The first call cannot compile because `ShippingCalculator` does not exist:

```csharp
var cost = new ShippingCalculator().Cost(2m);
```

Running `dotnet test` reports `error CS0246: The type or namespace name 'ShippingCalculator' could not be found`. We will use that error to shape the type one failing test at a time. The finished solution is in `fundamentals/methods`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Methods`, an MSTest project in `tests/Methods.Tests`, a solution and a project reference.

## How it works

A method takes input and returns a value. A property stores data that callers can configure. The calculator will expose a method with this shape:

```csharp
public decimal Cost(decimal weightKg)
```

- `public` lets callers outside the class use the method.
- `decimal` is the return type.
- `Cost` is the method name.
- `(decimal weightKg)` is one parameter named `weightKg` with type `decimal`.

The caller invokes the method on a calculator instance, as in `calculator.Cost(2m)`, and receives a `decimal` result.

## How to test

This calculation has no file, network or clock dependency. Given a weight and rate, the result is always the same, so the test can call the method directly and compare its decimal result. We will use MSTest to create the calculator and assert the expected cost.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Methods.Tests;

[TestClass]
public sealed class ShippingCalculatorTests
{
    [TestMethod]
    public void Cost_StandardShipping_ChargesTheRatePerKg()
    {
        var calculator = new ShippingCalculator();

        Assert.AreEqual(10m, calculator.Cost(2m));
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0246: The type or namespace name 'ShippingCalculator' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Methods;

public class ShippingCalculator
{
    public decimal Cost(decimal weightKg)
    {
        return 0;
    }
}
```

The test compiles and fails with `Expected:<10>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public decimal Cost(decimal weightKg)
{
    return weightKg * 5m;
}
```

Green. The test passes `2m` as the argument, and `Assert.AreEqual` compares the returned decimal with `10m`. The rate `5m` is still inside the method; the next requirement gives it a name the caller can change.

## Refactor

There is nothing to extract from a single multiplication. Keeping the method direct makes the calculation easier to read. Run the test again; it remains green.

## Repeat for new requirements

### Make the rate configurable

Write the test:

```csharp
[TestMethod]
public void Cost_UsesTheConfiguredRate()
{
    var calculator = new ShippingCalculator { RatePerKg = 8m };

    Assert.AreEqual(16m, calculator.Cost(2m));
}
```

Run the tests. The compiler says `ShippingCalculator` does not contain `RatePerKg`. Add an auto-property with a default value:

```csharp
public decimal RatePerKg { get; set; } = 5m;
```

Use it in the method:

```csharp
public decimal Cost(decimal weightKg)
{
    return weightKg * RatePerKg;
}
```

Green. `{ get; set; }` is an auto-implemented property: the compiler creates the backing field, the getter and the setter. The `= 5m` initializer gives every new calculator a default rate. The test uses an object initializer, `new ShippingCalculator { RatePerKg = 8m }`, which runs after the constructor and before the variable is used.

The property and the method have different jobs. The property holds configuration; the method computes behavior from it. Keeping the rate out of the method means a caller can change it without editing the class.

### Add express shipping

Write the test:

```csharp
[TestMethod]
public void Cost_ExpressShipping_AddsTheSurcharge()
{
    var calculator = new ShippingCalculator();

    Assert.AreEqual(30m, calculator.Cost(2m, express: true));
}
```

Run the tests. The compiler says there is no overload of `Cost` that takes two arguments. Add a second parameter with a default value:

```csharp
public decimal Cost(decimal weightKg, bool express = false)
{
    var cost = weightKg * RatePerKg;

    return cost;
}
```

The code compiles and the test fails with `Expected:<30>. Actual:<10>`. The parameter is there but unused. Add the surcharge:

```csharp
private const decimal ExpressSurcharge = 20m;

public decimal Cost(decimal weightKg, bool express = false)
{
    var cost = weightKg * RatePerKg;

    if (express)
    {
        cost += ExpressSurcharge;
    }

    return cost;
}
```

Green, and the original test still passes because `express` defaults to `false`.

`ExpressSurcharge` is private, so the constant has a name without becoming caller configuration. `RatePerKg` stays public because the caller can choose it. The method updates one local `cost` and returns once.

The call `Cost(2m, express: true)` uses a named argument. The same call could be written `Cost(2m, true)`, but the name says what the `true` means at the call site. Named arguments are most useful when a method has several parameters of the same type or a boolean that would otherwise be a mystery.

An optional parameter is one way to support two shapes of a call. Another is an overload: two methods with the same name and different parameter lists, like the `Divide` methods in the types chapter. Overloads are the right tool when the parameter types differ; a default value is simpler when one call is just the common case of the other.

## Wrapping up

What we have covered:

- Defining an instance method with parameters, a return type and a body.
- Calling a method from a test and comparing its return value.
- Auto-properties with `get`/`set` and an initializer.
- Object initializers to configure a new instance.
- Optional parameters and named arguments.
- A private constant and the difference between configuration and behavior.

The chapter follows the methods section of [Structs, Methods and Interfaces in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): a small type, a method that computes something useful, and tests that pin the result. .NET adds properties and named arguments to the same idea.

Next: [Classes and Objects](../classes/classes.md).

