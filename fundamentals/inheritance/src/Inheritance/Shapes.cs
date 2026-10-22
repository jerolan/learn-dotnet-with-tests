namespace Inheritance;

public interface IShape
{
    string Name { get; }

    double Area();
}

public abstract class Shape : IShape
{
    protected Shape(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract double Area();
}

public sealed class Rectangle : Shape
{
    private readonly double _width;
    private readonly double _height;

    public Rectangle(double width, double height)
        : base("Rectangle")
    {
        _width = width;
        _height = height;
    }

    public override double Area()
    {
        return _width * _height;
    }
}

public sealed class Circle : Shape
{
    private readonly double _radius;

    public Circle(double radius)
        : base("Circle")
    {
        _radius = radius;
    }

    public override double Area()
    {
        return Math.PI * _radius * _radius;
    }
}

public static class AreaCalculator
{
    public static double Total(IEnumerable<IShape> shapes)
    {
        var total = 0.0;

        foreach (var shape in shapes)
        {
            total += shape.Area();
        }

        return total;
    }
}

