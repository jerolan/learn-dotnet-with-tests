# Refactoring Checklist

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/refactoring-checklist)

Refactoring changes the structure of code without changing its behavior. The tests are the proof that behavior stayed the same. A checklist keeps the change small and stops the "while I am here" edits that turn a cleanup into a rewrite.

We will take a working discount calculation and clean it up with the tests green. The finished solution is in `testing/refactoring-checklist`.

## Write the tests first

```csharp
[TestClass]
public sealed class DiscountsTests
{
    [TestMethod]
    public void Apply_WithPercent_SubtractsItFromThePrice()
    {
        Assert.AreEqual(90m, Discounts.Apply(100m, 10, member: false));
    }

    [TestMethod]
    public void Apply_WithMember_AddsTheExtraDiscount()
    {
        Assert.AreEqual(85.5m, Discounts.Apply(100m, 10, member: true));
    }

    [TestMethod]
    public void Apply_WithInvalidPercent_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => Discounts.Apply(100m, 101, member: false));
    }
}
```

The implementation is deliberately plain:

```csharp
public static decimal Apply(decimal price, int percent, bool member)
{
    if (percent < 0 || percent > 100)
    {
        throw new ArgumentOutOfRangeException(nameof(percent));
    }

    var d = price - price * percent / 100;

    if (member)
    {
        d = d - d * 0.05m;
    }

    return d;
}
```

All three tests pass. Now the refactor starts from a known green state.

## The checklist

Work through these in order, running the tests after each change:

1. Are the tests green before you touch anything?
2. Does each name say what the value means?
3. Is there a magic number or string that deserves a name?
4. Is there a calculation or condition that would read better in another form?
5. Is there duplication that a small extraction would remove?
6. Are the guard clauses at the top, before the work?
7. Is there dead code, an unused variable or a comment that repeats the code?
8. Did the tests stay unchanged and green?
9. Is this a good point to commit?

If a test has to change, stop. That is a behavior change, not a refactor; it needs its own red-green cycle.

## Apply the checklist

The first pass is names. `d` does not say what it holds. Rename it to `afterPercent`:

```csharp
var afterPercent = price - price * percent / 100;
```

Run the tests. Green.

The next pass is the magic number. `0.05m` is the member discount; give it a name:

```csharp
private const decimal MemberDiscount = 0.05m;
```

Use it:

```csharp
if (member)
{
    afterPercent = afterPercent - afterPercent * MemberDiscount;
}
```

Green. The constant also documents that the discount is a rate, not an amount.

The calculation repeats `price` and `afterPercent`. A factor reads better:

```csharp
var afterPercent = price * (1 - percent / 100m);
```

and for the member step:

```csharp
afterPercent * (1 - MemberDiscount)
```

Green. The percent division uses `100m` so the arithmetic stays decimal; `100` would still work through promotion, but the suffix makes the intent explicit.

The member branch is now a single expression. A conditional expression makes the return read as one decision:

```csharp
return member
    ? afterPercent * (1 - MemberDiscount)
    : afterPercent;
```

Green. If the branch had more steps, an `if` would still be clearer; the checklist is not a rule to use a ternary everywhere.

The final version:

```csharp
public static class Discounts
{
    private const decimal MemberDiscount = 0.05m;

    public static decimal Apply(decimal price, int percent, bool member)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent));
        }

        var afterPercent = price * (1 - percent / 100m);

        return member
            ? afterPercent * (1 - MemberDiscount)
            : afterPercent;
    }
}
```

The guard uses a relational pattern, `percent is < 0 or > 100`. It says the same thing as the two comparisons and keeps the condition in one place. This is a readability choice; either form is correct.

## What not to do

Do not extract a method just to make the class look busier. `ApplyPercent` and `ApplyMemberDiscount` would each be a one-line method used once. That is indirection without a concept; the checklist asks for names that clarify, not for more layers.

Do not mix a rename with a behavior change. If the member discount should apply before the percent discount, write a failing test for the new order, make it pass, and only then refactor. The tests tell you which change you are making.

Do not refactor without a green baseline. If the suite is red, you cannot tell whether a failure came from the refactor or from the existing bug.

## Wrapping up

What we have covered:

- A refactor that changes names, constants and expressions while behavior stays the same.
- The checklist: green baseline, names, magic values, duplication, guards, dead code, rerun, commit.
- The difference between a refactor and a behavior change.
- Relational patterns and decimal arithmetic as readability choices.
- When an extraction adds a concept and when it only adds noise.

The chapter follows the [Refactoring checklist in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/refactoring-checklist): small steps, tests green, commit when the code reads better. The checklist is the same; the examples are C#.

That completes the testing fundamentals. The next part builds an application with these tools.

