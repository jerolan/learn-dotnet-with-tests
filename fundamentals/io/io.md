# Files and I/O

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/io)

I/O is reading from and writing to something outside the program: a file, a network stream, standard input. .NET models it with readers and writers, so the same parsing code can work against a file in production and an in-memory string in a test.

We will build a small CSV reader and writer over `TextReader` and `TextWriter`. The finished solution is in `fundamentals/io`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Io`, an MSTest project in `tests/Io.Tests`, a solution and a project reference.

## How it works

`TextReader` and `TextWriter` describe where characters come from and where they go without tying the code to a file. `ReadLine()` reads until the next line or returns `null` at the end; `WriteLine()` appends a line using the writer's newline sequence.

## How to test

Use `StringReader` and `StringWriter` to exercise the same reader and writer in memory. The test can check parsed rows and output without creating files or depending on the operating system's newline convention.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Io.Tests;

[TestClass]
public sealed class CsvTests
{
    [TestMethod]
    public void Read_ParsesRows()
    {
        var rows = Csv.Read(new StringReader("a,b\nc,d"));

        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
    }
}
```

`StringReader` is a `TextReader` over a string. The test does not touch the disk.

## Try to run the test

```text
error CS0246: The type or namespace name 'Csv' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Io;

public static class Csv
{
    public static IReadOnlyList<string[]> Read(TextReader reader)
    {
        return Array.Empty<string[]>();
    }
}
```

The test compiles and fails with `Expected:<2>. Actual:<0>`.

## Write enough code to make it pass

```csharp
public static IReadOnlyList<string[]> Read(TextReader reader)
{
    var rows = new List<string[]>();
    string? line;

    while ((line = reader.ReadLine()) != null)
    {
        rows.Add(line.Split(','));
    }

    return rows;
}
```

Green. `ReadLine()` returns the next line or `null` at the end of the input. The `while` condition both assigns the line and checks for the end. Each line is split on commas into a row.

The method accepts a `TextReader`, not a file path. That is the seam: a real file, a memory string and a network stream all provide a `TextReader`.

## Refactor

The reader already depends on the `TextReader` abstraction, so it can be tested without file-system setup. Run the test again; there is no extra layer to extract.

## Repeat for new requirements

### Blank lines

Write the test:

```csharp
[TestMethod]
public void Read_SkipsBlankLines()
{
    var rows = Csv.Read(new StringReader("a,b\n\n  \nc,d"));

    Assert.AreEqual(2, rows.Count);
}
```

It fails because the blank and whitespace-only lines become empty rows. Skip them:

```csharp
if (string.IsNullOrWhiteSpace(line))
{
    continue;
}

rows.Add(line.Split(','));
```

Green.

### Writing

Write the test:

```csharp
[TestMethod]
public void Write_WritesRows()
{
    var writer = new StringWriter();

    Csv.Write(writer, new[]
    {
        new[] { "a", "b" },
        new[] { "c", "d" }
    });

    var expected = "a,b" + Environment.NewLine + "c,d" + Environment.NewLine;

    Assert.AreEqual(expected, writer.ToString());
}
```

Add the method:

```csharp
public static void Write(TextWriter writer, IEnumerable<string[]> rows)
{
    foreach (var row in rows)
    {
        writer.WriteLine(string.Join(",", row));
    }
}
```

Green. `StringWriter` captures the output in memory. `WriteLine` uses the writer's newline sequence, so the test compares against `Environment.NewLine` instead of hard-coding `\n`.

### A round trip

Write the test:

```csharp
[TestMethod]
public void RoundTrip_PreservesRows()
{
    var writer = new StringWriter();

    Csv.Write(writer, new[] { new[] { "a", "b" } });

    var rows = Csv.Read(new StringReader(writer.ToString()));

    CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
}
```

It passes. The writer and the reader agree on the format, and the test verifies both halves together.

### Design limits and real files

The parser is deliberately simple. It splits on commas and does not handle quoted fields, embedded commas or embedded newlines. A real CSV file needs a parser that understands quoting; the point here is the reader/writer seam, not CSV correctness. Say so in the code or use a library when the format matters.

For a real file, open a reader or writer and dispose it:

```csharp
using var reader = File.OpenText("orders.csv");
var rows = Csv.Read(reader);
```

`File.OpenText` returns a `StreamReader`, which is a `TextReader`. `File.CreateText` returns a `StreamWriter`. The `using` statement closes the file even if parsing throws; the next chapter covers it.

Streaming with `ReadLine()` keeps only one line in memory. `File.ReadAllLines` loads the whole file, which is fine for a small file and wrong for a large one. The same choice appears with async I/O: `ReadLineAsync()` and `WriteLineAsync()` let the caller wait without blocking a thread.

## Wrapping up

What we have covered:

- `TextReader` and `TextWriter` as the seam over files and memory.
- `StringReader` and `StringWriter` for tests without disk I/O.
- `ReadLine()` returning `null` at the end of the input.
- Splitting and joining simple CSV rows.
- Skipping blank lines.
- A round-trip test across the reader and the writer.
- Streaming versus reading a whole file, and the `using` requirement for file handles.

The chapter follows the [I/O chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/io): the domain code reads from an abstract source, and a test supplies an in-memory one. .NET splits the abstraction into `TextReader` and `TextWriter`.

Next: [The using keyword](../using/using.md).

