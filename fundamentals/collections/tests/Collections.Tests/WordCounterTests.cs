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

    [TestMethod]
    public void Counts_IgnoresExtraWhitespace()
    {
        var counter = new WordCounter("  the\tcat\nand  the  ");

        Assert.AreEqual(2, counter.Counts["the"]);
        Assert.AreEqual(1, counter.Counts["cat"]);
    }

    [TestMethod]
    public void Counts_IsCaseInsensitive()
    {
        var counter = new WordCounter("The the THE");

        Assert.AreEqual(3, counter.Counts["the"]);
    }

    [TestMethod]
    public void MostCommon_ReturnsTheMostFrequentWords()
    {
        var counter = new WordCounter("the cat and the dog");

        var top = counter.MostCommon(2);

        Assert.AreEqual(2, top.Count);
        Assert.AreEqual("the", top[0]);
        Assert.AreEqual("and", top[1]);
    }

    [TestMethod]
    public void MostCommon_ReturnsEverythingWhenAskedForMore()
    {
        var counter = new WordCounter("a b");

        var top = counter.MostCommon(10);

        Assert.AreEqual(2, top.Count);
        Assert.AreEqual("a", top[0]);
        Assert.AreEqual("b", top[1]);
    }
}

