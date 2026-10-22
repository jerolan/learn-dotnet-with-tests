# Collections

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/collections)

An array has a fixed length. A `List<T>` grows. A `Dictionary<TKey, TValue>` looks values up by key, and a `HashSet<T>` answers "is this value present?" quickly. Choosing the right one changes how the code reads and how fast it runs.

We will build a word counter: read a text, count each word, and return the most common ones. The finished solution is in `fundamentals/collections`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Collections`, an MSTest project in `tests/Collections.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Collections.Tests;

[TestClass]
public sealed class WordCounterTests
{
    [TestMethod]
    public void Counts_CountsEachWord()
    {
        var counter = new WordCounter("the cat and the dog");

        Assert.AreEqual(2, counter.Counts["the"]);
        Assert.AreEqual(1, counter.Counts["cat"]);
        Assert.AreEqual(1, counter.Counts["and"]);
        Assert.AreEqual(1, counter.Counts["dog"]);
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'WordCounter' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Start with the shape of the result:

```csharp
namespace Collections;

public sealed class WordCounter
{
    public WordCounter(string text)
    {
    }

    public IReadOnlyDictionary<string, int> Counts { get; } =
        new Dictionary<string, int>();
}
```

The code compiles and the test fails with a `KeyNotFoundException` for `the`. The dictionary is empty.

## Write enough code to make it pass

```csharp
public sealed class WordCounter
{
    private readonly Dictionary<string, int> _counts;

    public WordCounter(string text)
    {
        _counts = CountWords(text);
    }

    public IReadOnlyDictionary<string, int> Counts => _counts;

    private static Dictionary<string, int> CountWords(string text)
    {
        var counts = new Dictionary<string, int>();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            counts.TryGetValue(word, out var current);
            counts[word] = current + 1;
        }

        return counts;
    }
}
```

Green. `Dictionary<string, int>` maps a word to its count. `TryGetValue` looks up the current count and returns `false` when the key is new; `out var current` gives you `0` in that case. The indexer assignment then creates the entry or updates it.

`IReadOnlyDictionary` exposes the map without letting callers add or remove entries. The field stays private and mutable; the property is the public view.

## Repeat for new requirements

### More than spaces

Write the test:

```csharp
[TestMethod]
public void Counts_IgnoresExtraWhitespace()
{
    var counter = new WordCounter("  the\tcat\nand  the  ");

    Assert.AreEqual(2, counter.Counts["the"]);
    Assert.AreEqual(1, counter.Counts["cat"]);
}
```

It fails because the split only knows the space character: `cat` arrives as `"cat\nand"`, and the count for `cat` is wrong. Give the split an explicit set of separators:

```csharp
var separators = new[] { ' ', '\t', '\n', '\r' };

foreach (var word in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
```

Green. `RemoveEmptyEntries` drops the empty strings produced by repeated separators; without it, the leading and doubled spaces would create empty keys.

### Case does not create a second word

Write the test:

```csharp
[TestMethod]
public void Counts_IsCaseInsensitive()
{
    var counter = new WordCounter("The the THE");

    Assert.AreEqual(3, counter.Counts["the"]);
}
```

It fails with `Expected:<3>. Actual:<2>` because `The` and `THE` are different keys. Normalize before the lookup:

```csharp
var key = word.ToLowerInvariant();

counts.TryGetValue(key, out var current);
counts[key] = current + 1;
```

Green. `ToLowerInvariant` avoids the current culture's casing rules, so the count does not change with the machine's locale.

### The most common words

A dictionary is not ordered by value, so the ranking needs a separate list. Write the test:

```csharp
[TestMethod]
public void MostCommon_ReturnsTheMostFrequentWords()
{
    var counter = new WordCounter("the cat and the dog");

    var top = counter.MostCommon(2);

    Assert.AreEqual(2, top.Count);
    Assert.AreEqual("the", top[0]);
    Assert.AreEqual("and", top[1]);
}
```

The compiler says `MostCommon` is missing. Add it:

```csharp
public IReadOnlyList<string> MostCommon(int count)
{
    var entries = new List<KeyValuePair<string, int>>(_counts);

    entries.Sort((left, right) =>
    {
        var byCount = right.Value.CompareTo(left.Value);

        return byCount != 0
            ? byCount
            : string.CompareOrdinal(left.Key, right.Key);
    });

    var result = new List<string>();

    for (var i = 0; i < count && i < entries.Count; i++)
    {
        result.Add(entries[i].Key);
    }

    return result;
}
```

Green. `new List<KeyValuePair<string, int>>(_counts)` copies the dictionary into a list that can be sorted. The comparison puts the higher count first; when the counts are equal, it compares the keys so the result is deterministic. The loop stops at `count` or at the end of the list, whichever comes first.

Add the boundary:

```csharp
[TestMethod]
public void MostCommon_ReturnsEverythingWhenAskedForMore()
{
    var counter = new WordCounter("a b");

    var top = counter.MostCommon(10);

    Assert.AreEqual(2, top.Count);
    Assert.AreEqual("a", top[0]);
    Assert.AreEqual("b", top[1]);
}
```

It passes because of the `i < entries.Count` condition.

## Refactor

The code is small and each collection has a job:

- `Dictionary<string, int>` stores the counts and gives constant-time lookup by word.
- `List<KeyValuePair<string, int>>` is the sortable copy. Sorting a list is allowed; sorting a dictionary directly is not, because a dictionary has no order.
- `IReadOnlyDictionary` and `IReadOnlyList` are the public contracts. They say the caller can read but not mutate the internal storage.

An array would be a poor fit for the counts: the keys are words, not indexes, and the number of distinct words is unknown until the text is read. A `List<string>` would need a linear search for every word, which turns the count into quadratic work. A `HashSet<string>` is the right collection when you only need membership, not a count; it answers `Contains` quickly and rejects duplicates.

## Wrapping up

What we have covered:

- `Dictionary<TKey, TValue>` for counts and `TryGetValue` with an `out` variable.
- `string.Split` with explicit whitespace separators and `RemoveEmptyEntries`.
- Case normalization with `ToLowerInvariant`.
- `List<T>` as a sortable copy of a dictionary.
- `Sort` with a comparison that breaks ties deterministically.
- `IReadOnlyDictionary` and `IReadOnlyList` as public views.
- When an array, a list, a dictionary or a set is the right choice.

The chapter follows the [Maps chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/maps): a map stores counts, a missing key is part of the behavior, and the tests pin the lookup. .NET adds the read-only interfaces and the sorted-list step for a ranking.

Next: [IEnumerable](../enumerable/enumerable.md).

