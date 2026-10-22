# Herencia e interfaces

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/inheritance)

La herencia deja que una clase reutilice estado y comportamiento de una clase base. Una interfaz define un contrato sin decir cómo se implementa. Las dos permiten que quien llama trabaje con un grupo de tipos a través de una sola forma, y resuelven partes distintas del problema.

Vamos a construir dos figuras, encontrar el contrato que comparten y después calcular un total a través de ese contrato. La solución terminada está en `fundamentals/inheritance`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Inheritance`, un proyecto de MSTest en `tests/Inheritance.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
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
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Rectangle' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Inheritance;

public sealed class Rectangle
{
    public Rectangle(double width, double height)
    {
    }

    public double Area()
    {
        return 0;
    }
}
```

El código compila y el test falla con `Expected:<12>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public sealed class Rectangle
{
    private readonly double _width;
    private readonly double _height;

    public Rectangle(double width, double height)
    {
        _width = width;
        _height = height;
    }

    public double Area()
    {
        return _width * _height;
    }
}
```

Verde.

## Repite con requisitos nuevos

### Una segunda figura

Escribe el test:

```csharp
[TestMethod]
public void Circle_Area_UsesPi()
{
    Assert.AreEqual(Math.PI, new Circle(1).Area(), 0.0001);
}
```

Agrega la clase con un área de marcador, mírala fallar con `Expected:<3.14159...>. Actual:<0>` y después implementa:

```csharp
public sealed class Circle
{
    private readonly double _radius;

    public Circle(double radius)
    {
        _radius = radius;
    }

    public double Area()
    {
        return Math.PI * _radius * _radius;
    }
}
```

Verde. El tercer argumento de `Assert.AreEqual` es una tolerancia. Comparar valores de punto flotante de forma exacta es mala idea, así que el test acepta una diferencia de `0.0001`.

### Encontrar el contrato

`Rectangle` y `Circle` tienen la misma firma de método y ningún estado compartido. Eso es una interfaz: la promesa de que un tipo puede hacer algo, sin clase base.

Extráela:

```csharp
public interface IShape
{
    double Area();
}
```

y declara `Rectangle : IShape` y `Circle : IShape`. Ejecuta los tests. Verde, sin cambios de comportamiento. La interfaz es ahora el tipo que puede usar quien solo necesita un área.

### Darle un nombre a cada figura

Escribe el test:

```csharp
[TestMethod]
public void Shapes_HaveNames()
{
    Assert.AreEqual("Rectangle", new Rectangle(1, 1).Name);
    Assert.AreEqual("Circle", new Circle(1).Name);
}
```

El compilador dice que `Rectangle` no tiene `Name`. Agrega la propiedad al contrato:

```csharp
public interface IShape
{
    string Name { get; }

    double Area();
}
```

Ahora el compilador dice que las dos clases no implementan `IShape.Name`. El nombre lo comparten todas las figuras, así que este es el punto donde una clase base se gana su lugar:

```csharp
public abstract class Shape : IShape
{
    protected Shape(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract double Area();
}
```

`Shape` implementa la interfaz y provee `Name`. `Area` queda abstracta, porque la clase base no conoce la fórmula. Haz que las clases concretas hereden de ella:

```csharp
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
```

Verde. Aquí hay varias cosas nuevas:

- `abstract` en `Shape` significa que no puedes escribir `new Shape("x")`. La clase existe para ser heredada.
- El constructor `protected` se puede llamar desde una clase derivada, no desde fuera.
- `: base("Rectangle")` ejecuta el constructor de la clase base y le pasa el nombre.
- `override` reemplaza la `Area` abstracta. El compilador lo exige.

El nombre vive en un solo lugar, y las dos figuras lo heredan. El área queda en cada figura porque solo la figura conoce su fórmula.

## Polimorfismo

Escribe el test:

```csharp
[TestMethod]
public void Total_SumsEveryShapeInTheList()
{
    var shapes = new IShape[] { new Rectangle(3, 4), new Circle(1) };

    Assert.AreEqual(12 + Math.PI, AreaCalculator.Total(shapes), 0.0001);
}
```

El compilador dice que `AreaCalculator` no existe. Agrégalo:

```csharp
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
```

Verde. `AreaCalculator` nunca menciona `Rectangle` ni `Circle`. Solo conoce `IShape`, llama a `Area()`, y el runtime elige la implementación correcta para cada objeto. Eso es polimorfismo: un lugar de llamada, muchos comportamientos.

La misma idea funciona con la clase base: un `Shape[]` puede guardar las dos figuras y llamar a la propiedad `Name` heredada.

## Refactor

La jerarquía es pequeña y cada pieza tiene una sola razón para existir: la interfaz nombra el contrato, la clase abstracta guarda el estado compartido y las clases concretas proveen las fórmulas.

Una clase puede implementar muchas interfaces pero heredar de una sola clase base. Cuando un tipo necesita usarse a través de más de un contrato, las interfaces son el camino. Cuando varios tipos comparten estado o una plantilla de comportamiento, una clase base evita repetirlo.

`sealed` en `Rectangle` y `Circle` dice que no se espera que nadie herede de ellas. Si más adelante una figura necesita una subclase, quita `sealed` a propósito.

## Para cerrar

Lo que cubrimos:

- Dos figuras concretas y sus fórmulas de área.
- Una interfaz como contrato sin implementación.
- Una clase base abstracta que implementa parte del contrato y deja el resto abstracto.
- Constructores `protected`, llamadas `base(...)` y `override`.
- Polimorfismo a través de una interfaz, con un bucle que llama a muchas implementaciones.
- El intercambio entre clase base e interfaz.

El capítulo sigue la sección de interfaces de [Structs, Methods and Interfaces en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): define el comportamiento que necesitas y deja que cada tipo lo provea. .NET agrega la herencia de clases sobre el contrato de la interfaz.

Siguiente: [Composición sobre herencia](../composition/composition.es.md).

