using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Types.Tests;

[TestClass]
public sealed class TypeExamplesTests
{
    [TestMethod]
    public void Add_TwoIntegers_ReturnsSum()
    {
        Assert.AreEqual(8, TypeExamples.Add(5, 3));
    }

    [TestMethod]
    public void IsEven_EvenNumber_ReturnsTrue()
    {
        Assert.IsTrue(TypeExamples.IsEven(4));
        Assert.IsFalse(TypeExamples.IsEven(5));
    }

    [TestMethod]
    public void Describe_KnownTypes_ReturnsTaggedText()
    {
        Assert.AreEqual("int:5", TypeExamples.Describe(5));
        Assert.AreEqual("bool:True", TypeExamples.Describe(true));
    }
}
