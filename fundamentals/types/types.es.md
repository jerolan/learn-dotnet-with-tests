# Tipos y variables

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/types)

Sumar dos enteros debe devolver su suma: `TypeExamples.Add(2, 2)` debe producir `4`.

La llamada todavía no compila porque `TypeExamples` no existe:

```csharp
var sum = TypeExamples.Add(2, 2);
```

Al ejecutar `dotnet test`, aparece `error CS0103: The name 'TypeExamples' does not exist in the current context`. Empezaremos con enteros y luego usaremos más tipos para ver cómo C# valida los argumentos y elige un método. La solución terminada está en `fundamentals/types`. Si la estás construyendo desde cero, ármala igual que en Hello, World: una biblioteca de clases en `src/Types`, un proyecto de MSTest en `tests/Types.Tests`, una solución y una referencia del proyecto de tests a la biblioteca. Después crea `tests/Types.Tests/TypeExamplesTests.cs`.

## Cómo funciona

C# tiene tipado estático: el compilador conoce el tipo de cada variable antes de ejecutar el programa. `TypeExamples` será una clase estática, así que se puede llamar a su método `Add` sin crear un objeto. El método recibirá dos valores `int` y devolverá un `int`; `var` permite que el compilador infiera el tipo de una variable local a partir del valor que recibe.

## Cómo probarlo

`Add` no tiene efectos secundarios: las mismas dos entradas enteras siempre producen la misma suma. Un test unitario puede llamar al método directamente y comparar el valor devuelto. Las filas de datos de MSTest nos permitirán probar varios pares de entradas sin copiar la misma aserción.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Types.Tests;

[TestClass]
public sealed class TypeExamplesTests
{
    [TestMethod]
    public void Add()
    {
        var sum = TypeExamples.Add(2, 2);
        var expected = 4;

        Assert.AreEqual(expected, sum);
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`.

El proyecto no compila:

```text
error CS0103: The name 'TypeExamples' does not exist in the current context
```

Ese es el compilador diciéndonos que falta el tipo. Todavía no lo escribimos.

## Escribe el código mínimo para que el test compile y mira la salida

Escribe lo justo para satisfacer al compilador y nada más:

```csharp
namespace Types;

public static class TypeExamples
{
    public static int Add(int x, int y)
    {
        return 0;
    }
}
```

Ahora ejecuta los tests. El código compila y el test falla por la razón correcta:

```text
  Failed Add [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<4>. Actual:<0>.
```

Queremos ver ese fallo. Demuestra que el test está conectado al método y comprueba el resultado, no solo que el método existe.

## Escribe el código suficiente para que pase

En el sentido más estricto de TDD, ahora escribimos el código mínimo que hace pasar el test. Un programador pedante podría escribir:

```csharp
public static int Add(int x, int y)
{
    return 4;
}
```

TDD es un fraude, ¿no? El test pasa.

El problema es que el test usó un solo par de números. Podríamos agregar otro test con números distintos, pero perseguir una respuesta fija test por test se convierte en un juego del gato y el ratón.

Por ahora, arregla el método de verdad:

```csharp
public static int Add(int x, int y)
{
    return x + y;
}
```

Vuelve a ejecutar los tests. Verde.

## Refactor

No hay mucho código que mejorar. Es un buen momento para mirar la firma del método. `int Add(int x, int y)` dice qué entra y qué sale; los nombres ayudan cuando la lees en el editor.

C# tiene comentarios de documentación XML. Aparecen en IntelliSense y en la documentación generada:

```csharp
/// <summary>Adds two integers.</summary>
public static int Add(int x, int y)
{
    return x + y;
}
```

El libro de Go usa ejemplos ejecutables aquí. C# no ejecuta ejemplos desde los comentarios de documentación, así que usamos una tabla de casos. Reemplaza el test único de `Add` por:

```csharp
[DataTestMethod]
[DataRow(2, 2, 4)]
[DataRow(1, 5, 6)]
[DataRow(-3, 3, 0)]
public void Add_ReturnsSum(int x, int y, int want)
{
    Assert.AreEqual(want, TypeExamples.Add(x, y));
}
```

`[DataTestMethod]` con `[DataRow]` es la versión de MSTest de un test con tabla. Cada fila corre como un caso separado, y un `return 4` fijo fallaría dos de ellas.

## Repite con requisitos nuevos

### División entera

El siguiente requisito es la división. Escribe primero el test:

```csharp
[TestMethod]
public void DivideIntegers_TruncatesTheFraction()
{
    Assert.AreEqual(2, TypeExamples.Divide(10, 4));
}
```

Ejecuta los tests. El compilador se queja de que `TypeExamples` no contiene `Divide`.

Agrega el método con el cuerpo más pequeño que compile:

```csharp
public static int Divide(int x, int y)
{
    return 0;
}
```

El test ahora falla con `Expected:<2>. Actual:<0>`. Hazlo pasar:

```csharp
public static int Divide(int x, int y)
{
    return x / y;
}
```

Verde. Fíjate en lo que pasó con la fracción: `10 / 4` es `2`, no `2.5`. Cuando ambos operandos son `int`, C# hace división entera y descarta el resto. Es correcto para el tipo, y es una fuente habitual de errores cuando esperabas una fracción.

### Doubles

Si queremos la fracción, necesitamos un tipo que pueda representarla. Escribe el test:

```csharp
[TestMethod]
public void DivideDoubles_KeepsTheFraction()
{
    Assert.AreEqual(2.5, TypeExamples.Divide(10.0, 4.0));
}
```

`10.0` y `4.0` son literales `double`, así que la llamada busca un `Divide` que reciba dos `double`. El compilador dice que no existe esa sobrecarga.

Agrégala:

```csharp
public static double Divide(double x, double y)
{
    return x / y;
}
```

Ejecuta los tests. Pasan los dos tests de división. C# eligió el método correcto a partir de los tipos de los argumentos: `10` es `int`, `10.0` es `double`. Dos métodos con el mismo nombre y distintos tipos de parámetros son sobrecargas.

### Booleanos

Un tipo más antes de seguir. Escribe un test para una pregunta de sí o no:

```csharp
[TestMethod]
public void IsPositive_ChecksTheSign()
{
    Assert.IsTrue(TypeExamples.IsPositive(5));
    Assert.IsFalse(TypeExamples.IsPositive(-1));
    Assert.IsFalse(TypeExamples.IsPositive(0));
}
```

No compila porque `IsPositive` no existe. Agrega el método:

```csharp
public static bool IsPositive(int value)
{
    return value > 0;
}
```

Ejecuta los tests. Verde. La comparación devuelve un `bool`, y `Assert.IsTrue` y `Assert.IsFalse` lo comprueban. El cero no es positivo ni negativo, por eso está la tercera aserción.

## Para cerrar

Lo que cubrimos:

- Más práctica con el ciclo TDD: rojo, verde, refactor.
- `int`, `double` y `bool`, y cómo la sintaxis del literal elige el tipo.
- La división entera trunca; la división de punto flotante conserva la fracción.
- Sobrecarga de métodos, para que `Divide` sirva a los dos tipos numéricos.
- `[DataTestMethod]` y `[DataRow]` para una tabla de casos.
- Comentarios de documentación XML como la respuesta de C# a Go doc.

El capítulo sigue el [capítulo Integers de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/integers): una función `Add` pequeña, un error del compilador, una implementación mínima deliberadamente incorrecta y después una de verdad. La tabla de casos y los tipos numéricos extra son la traducción a .NET de la misma lección.

Siguiente: [Decimales](../decimal/decimal.es.md), donde `double` deja de alcanzar.

