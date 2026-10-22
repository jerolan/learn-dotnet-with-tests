using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Enumerable.Tests;

[TestClass]
public sealed class SequenceTests
{
    [TestMethod]
    public void Naturals_FirstThreeAreZeroOneTwo()
    {
        var first = new List<int>();

        foreach (var number in Sequence.Naturals())
        {
            first.Add(number);

            if (first.Count == 3)
            {
                break;
            }
        }

        Assert.AreEqual(3, first.Count);
        Assert.AreEqual(0, first[0]);
        Assert.AreEqual(1, first[1]);
        Assert.AreEqual(2, first[2]);
    }

    [TestMethod]
    public void Evens_UpToTen_ReturnsTheEvenNumbers()
    {
        var evens = new List<int>();

        foreach (var number in Sequence.Evens(10))
        {
            evens.Add(number);
        }

        CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8, 10 }, evens);
    }

    [TestMethod]
    public void Tracked_DoesNotRunUntilEnumerated()
    {
        var log = new List<int>();
        var sequence = Sequence.Tracked(log);

        Assert.AreEqual(0, log.Count);

        using var enumerator = sequence.GetEnumerator();

        Assert.AreEqual(0, log.Count);

        enumerator.MoveNext();

        Assert.AreEqual(1, log.Count);
        Assert.AreEqual(1, enumerator.Current);
    }
}

