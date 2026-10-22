# Snapshot Testing with Verify

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/snapshot-verify)

A snapshot test writes the output of a function to a file, commits that file and fails when the output changes. It is useful for large, structured results that are tedious to assert field by field: formatted reports, serialized payloads, generated HTML.

This appendix uses [Verify](https://github.com/VerifyTests/Verify) with MSTest. The sample formats an order summary.

## Write the test first

```csharp
[TestClass]
public sealed class OrderFormatterTests : VerifyBase
{
    [TestMethod]
    public Task FormatsTheOrder()
    {
        var order = new[]
        {
            new OrderLine("Keyboard", 2, 49.95m),
            new OrderLine("Mouse", 1, 19.95m)
        };

        return Verify(OrderFormatter.Format(order));
    }
}
```

The class inherits `VerifyBase`. Verify.MSTest 16 is pinned because it works with MSTest 2.2.8; later Verify.MSTest versions require MSTest 3.x.

## The first run

The test fails because there is no verified file yet. Verify writes a `.received.txt` next to the test:

```text
Keyboard x2 @ 49.95
Mouse x1 @ 19.95
Total: 119.85
```

Read the output. If it is what you want, rename the received file to `OrderFormatterTests.FormatsTheOrder.verified.txt` and commit it. Run the test again; it passes.

That renaming is the approval step. A snapshot is not a magic assertion; it is a file you reviewed and accepted.

## How it works

On each run, Verify renders the value and compares it with the verified file. A mismatch fails the test and writes the received file for inspection. The diff shows exactly which lines changed.

The verified file lives in source control. A change to the formatter that alters the output is a behavior change, and the test makes you look at it. If the new output is correct, update the verified file in the same commit as the code change.

## A change to review

Change the total label:

```csharp
rows.Add("Grand total: " + total.ToString("0.00"));
```

Run the test. It fails and the received file shows the new line. Read the diff, then approve it by replacing the verified file. The snapshot caught a change that a single `Assert.AreEqual` would have missed if it only checked the product line.

## When a snapshot is the right tool

Use a snapshot when:

- The output is large or deeply structured.
- A field-by-field assertion would be noisy and hide the important change.
- The output is stable enough that a diff is meaningful.

Do not use a snapshot as a substitute for a behavior test. A snapshot says "the output changed"; it does not say "the total is correct" unless you read the file. Keep a small number of focused assertions for the rules that matter and use the snapshot for the shape.

## Refactor

A few settings keep snapshots maintainable:

- Scrub unstable values. A GUID, a timestamp or a machine path changes on every run. Replace it with a stable placeholder before verifying, or the snapshot will fail forever.
- Keep snapshots small. A hundred-line snapshot is rarely reviewed and often approved blindly.
- Put the verified files in source control and review their diffs like code.
- Use a separate snapshot per scenario, not one huge file for the whole application.
- Run the tests in CI. A missing verified file is a failure, not a skip.

The appendix's sample is deliberately deterministic: no clock, no random id, no paths. That is what makes the snapshot useful.

## Wrapping up

What we have covered:

- A snapshot test with Verify.MSTest and a verified file.
- The received/verified workflow and the approval step.
- Reviewing a diff when the output changes.
- When a snapshot is a good fit and when a behavior assertion is clearer.
- Scrubbers, file size and source control discipline.

