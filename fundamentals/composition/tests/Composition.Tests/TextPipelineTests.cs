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

    [TestMethod]
    public void Apply_WithNoSteps_ReturnsTheInput()
    {
        var pipeline = new TextPipeline(Array.Empty<ITextTransform>());

        Assert.AreEqual("hello", pipeline.Apply("hello"));
    }
}

