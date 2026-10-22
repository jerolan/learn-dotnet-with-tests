# Composition over Inheritance

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/composition)

Inheritance models an "is a" relationship: a rectangle is a shape. Composition models "uses a": a report uses a set of text transforms. The second relationship is often the one you want, because it lets you assemble behavior without creating a class for every combination.

We will build a text pipeline from small transforms. The finished solution is in `fundamentals/composition`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Composition`, an MSTest project in `tests/Composition.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Composition.Tests;

[TestClass]
public sealed class TextPipelineTests
{
    [TestMethod]
    public void Trim_RemovesTheSurroundingSpaces()
    {
        Assert.AreEqual("hello", new Trim().Apply("  hello  "));
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'Trim' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Start with the contract and a placeholder:

```csharp
namespace Composition;

public interface ITextTransform
{
    string Apply(string input);
}

public sealed class Trim : ITextTransform
{
    public string Apply(string input)
    {
        return input;
    }
}
```

The test fails with `Expected:<hello>. Actual:<  hello  >`.

## Write enough code to make it pass

```csharp
public string Apply(string input)
{
    return input.Trim();
}
```

Green. `ITextTransform` is the seam: a small contract with one method. Any step that can turn a string into a string can implement it.

## Repeat for new requirements

### Run several transforms

Write the test:

```csharp
[TestMethod]
public void Apply_RunsEveryStepInOrder()
{
    var pipeline = new TextPipeline(new ITextTransform[]
    {
        new UpperCase(),
        new Prefix("hi ")
    });

    Assert.AreEqual("hi HELLO", pipeline.Apply("hello"));
}
```

The compiler complains about `TextPipeline`, `UpperCase` and `Prefix`. Add the transforms:

```csharp
public sealed class UpperCase : ITextTransform
{
    public string Apply(string input)
    {
        return input.ToUpperInvariant();
    }
}

public sealed class Prefix : ITextTransform
{
    private readonly string _prefix;

    public Prefix(string prefix)
    {
        _prefix = prefix;
    }

    public string Apply(string input)
    {
        return _prefix + input;
    }
}
```

Then add the pipeline:

```csharp
public sealed class TextPipeline
{
    private readonly IEnumerable<ITextTransform> _steps;

    public TextPipeline(IEnumerable<ITextTransform> steps)
    {
        _steps = steps;
    }

    public string Apply(string input)
    {
        var result = input;

        foreach (var step in _steps)
        {
            result = step.Apply(result);
        }

        return result;
    }
}
```

Green. The pipeline does not inherit from `Trim` or `UpperCase`; it holds a list of steps and calls each one. The order of the list is the order of the transformations.

### The order matters

Write the test:

```csharp
[TestMethod]
public void Apply_UsesTheOrderOfTheSteps()
{
    var pipeline = new TextPipeline(new ITextTransform[]
    {
        new Prefix("hi "),
        new UpperCase()
    });

    Assert.AreEqual("HI HELLO", pipeline.Apply("hello"));
}
```

It passes. The first pipeline uppercased `hello` and then added the lowercase prefix; this one adds the prefix first, so the uppercase step also changes the prefix. The pipeline does not decide the order; the caller does.

### No steps

Write the boundary test:

```csharp
[TestMethod]
public void Apply_WithNoSteps_ReturnsTheInput()
{
    var pipeline = new TextPipeline(Array.Empty<ITextTransform>());

    Assert.AreEqual("hello", pipeline.Apply("hello"));
}
```

It passes. An empty list means the loop never runs and the input comes back unchanged. That is a useful default for a pipeline built from configuration.

## Refactor

The code is already small. The design lesson is what did not get created. An inheritance design would need a class for each combination:

```csharp
public sealed class TrimmedUpperCase : ITextTransform { ... }
public sealed class TrimmedUpperCaseWithPrefix : ITextTransform { ... }
public sealed class TrimmedWithPrefixAndUpperCase : ITextTransform { ... }
```

Every new transform multiplies the number of classes, and each class repeats the same loop. Composition replaces that set with one pipeline and a list of independent steps.

The seam is `ITextTransform`. Adding a `ReplaceSpaces` or a `Truncate` transform means writing one class and passing it in; `TextPipeline` does not change. That is the open/closed idea in practice: extend by adding a step, not by editing the pipeline.

The same constructor is also the setup for dependency injection. `TextPipeline` does not create its steps; it receives them. The next chapter looks at who creates them and how a container wires them together.

## Wrapping up

What we have covered:

- Composition as "uses a" instead of inheritance as "is a".
- A small interface as the seam between the pipeline and its steps.
- A pipeline that runs an ordered list of transforms.
- Order as part of the behavior, not an implementation detail.
- An empty list as a valid boundary.
- Why composition avoids the class explosion of a combination hierarchy.

The chapter builds on the interface section of [Structs, Methods and Interfaces in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): a small contract lets independent pieces fit together. The pipeline is the composition version of the shape hierarchy from the previous chapter.

Next: [Collections](../collections/collections.md).

