using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inheritance.Tests;

[TestClass]
public sealed class ShapeTests
{
    [TestMethod]
    public void Rectangle_Area_ReturnsWidthTimesHeight()
    {
        Assert.AreEqual(12.0, new Rectangle(3, 4).Area());
    }

    [TestMethod]
    public void Circle_Area_UsesPi()
    {
        Assert.AreEqual(Math.PI, new Circle(1).Area(), 0.0001);
    }

    [TestMethod]
    public void Shapes_HaveNames()
    {
        Assert.AreEqual("Rectangle", new Rectangle(1, 1).Name);
        Assert.AreEqual("Circle", new Circle(1).Name);
    }

    [TestMethod]
    public void Total_SumsEveryShapeInTheList()
    {
        var shapes = new IShape[] { new Rectangle(3, 4), new Circle(1) };

        Assert.AreEqual(12 + Math.PI, AreaCalculator.Total(shapes), 0.0001);
    }
}

