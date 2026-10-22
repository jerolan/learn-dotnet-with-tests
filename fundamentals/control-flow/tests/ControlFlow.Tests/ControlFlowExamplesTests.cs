using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ControlFlow.Tests;

[TestClass]
public sealed class ControlFlowExamplesTests
{
    [TestMethod]
    public void Repeat_ReturnsTheTextRepeated()
    {
        Assert.AreEqual("aaa", ControlFlowExamples.Repeat("a", 3));
    }

    [TestMethod]
    public void Repeat_ZeroTimes_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, ControlFlowExamples.Repeat("a", 0));
    }

    [TestMethod]
    public void SumTo_AddsTheNumbersUpToTheLimit()
    {
        Assert.AreEqual(10, ControlFlowExamples.SumTo(4));
    }

    [TestMethod]
    public void SumTo_Zero_ReturnsZero()
    {
        Assert.AreEqual(0, ControlFlowExamples.SumTo(0));
    }

    [DataTestMethod]
    [DataRow(1, "1")]
    [DataRow(3, "Fizz")]
    [DataRow(5, "Buzz")]
    [DataRow(15, "FizzBuzz")]
    public void FizzBuzz_ReturnsTheRightWord(int number, string want)
    {
        Assert.AreEqual(want, ControlFlowExamples.FizzBuzz(number));
    }
}

