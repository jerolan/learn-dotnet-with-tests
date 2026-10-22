# Excepciones

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/exceptions)

El formulario de registro recibe `"42"` y debe producir la edad `42`. Acepta números enteros de 0 a 120 y rechaza otras entradas con una `InvalidAgeException` que quien llama pueda manejar.

La primera llamada no compila porque `Age` no existe:

```csharp
var age = Age.Parse("42");
```

Al ejecutar `dotnet test`, aparece `error CS0103: The name 'Age' does not exist in the current context`. Usaremos el compilador y los tests que fallan para definir el contrato del parser. La solución terminada está en `fundamentals/exceptions`. Si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Exceptions`, un proyecto de MSTest en `tests/Exceptions.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

`int.Parse` convierte texto a entero, pero lanza `FormatException` cuando el texto tiene un formato incorrecto. `int.TryParse` informa si tuvo éxito mediante un `bool` y escribe el entero parseado en una variable `out`. Una excepción de dominio permite que el parser indique qué entrada inválida debe manejar quien lo llama.

## Cómo probarlo

El parser solo convierte una cadena en una edad o la rechaza. No depende de archivos, red ni reloj, así que un test unitario puede llamarlo directamente. Los tests comprobarán tanto la edad devuelta como el tipo de excepción para una entrada inválida.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exceptions.Tests;

[TestClass]
public sealed class AgeTests
{
    [TestMethod]
    public void Parse_Number_ReturnsTheAge()
    {
        Assert.AreEqual(42, Age.Parse("42"));
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0103: The name 'Age' does not exist in the current context
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Exceptions;

public static class Age
{
    public static int Parse(string text)
    {
        return 0;
    }
}
```

El test falla con `Expected:<42>. Actual:<0>`.

## Escribe el código suficiente para que pase

Deja que el framework parsee el número:

```csharp
public static int Parse(string text)
{
    return int.Parse(text);
}
```

Verde. `int.Parse` también lanza `FormatException` cuando el texto no es un número. Es un comportamiento útil, pero es la excepción equivocada para quienes nos llaman.

## Refactor

El camino válido es una llamada a `int.Parse`, así que todavía no hay nada que extraer. Ejecuta el test otra vez: sigue en verde. El siguiente test que falle definirá el tipo de excepción para texto inválido.

## Repite con requisitos nuevos

### El texto inválido necesita un error de dominio

Escribe el test para el camino de error:

```csharp
[TestMethod]
public void Parse_Text_ThrowsInvalidAge()
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse("abc"));
}
```

Ejecútalo. El test falla porque el método lanzó la excepción equivocada:

```text
Assert.ThrowsException failed. Threw exception FormatException, but exception InvalidAgeException was expected.
```

Ese es el punto del test. Quien llama pidió una edad; que la implementación haya usado `int.Parse` es un detalle interno. Introduce una excepción de dominio:

```csharp
namespace Exceptions;

public sealed class InvalidAgeException : Exception
{
    public InvalidAgeException(string message)
        : base(message)
    {
    }
}
```

Después usa `int.TryParse`, que devuelve un `bool` en vez de lanzar, y lanza nuestra propia excepción:

```csharp
public static int Parse(string text)
{
    if (!int.TryParse(text, out var age))
    {
        throw new InvalidAgeException($"'{text}' is not a number");
    }

    return age;
}
```

Verde. `out var age` declara la variable en el lugar de la llamada y deja que `TryParse` la asigne. Cuando el método devuelve `false`, nunca usamos `age` porque el guard lanza antes.

### Edades fuera de rango

El formulario tiene un rango. Escribe el test para una edad negativa:

```csharp
[TestMethod]
public void Parse_NegativeAge_ThrowsInvalidAge()
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse("-1"));
}
```

Falla con `No exception thrown. InvalidAgeException exception was expected.` porque `-1` sí parsea. Agrega el límite inferior:

```csharp
if (age < 0)
{
    throw new InvalidAgeException($"'{text}' is outside the valid age range");
}

return age;
```

Verde. Ahora el límite superior. Escribe un test con `130`, míralo fallar por la misma razón y extiende el guard:

```csharp
private const int MaxAge = 120;

public static int Parse(string text)
{
    if (!int.TryParse(text, out var age))
    {
        throw new InvalidAgeException($"'{text}' is not a number");
    }

    if (age < 0 || age > MaxAge)
    {
        throw new InvalidAgeException($"'{text}' is outside the valid age range");
    }

    return age;
}
```

Verde.

Los tres casos inválidos comprueban el mismo contrato con entradas distintas, así que van en una tabla:

```csharp
[DataTestMethod]
[DataRow("abc")]
[DataRow("-1")]
[DataRow("130")]
public void Parse_InvalidAge_ThrowsInvalidAge(string text)
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse(text));
}
```

### Mantener acotado el límite del error

El código de producción ya es pequeño: un parseo, dos cláusulas de guarda y un return. La constante le da nombre al límite superior, y los dos mensajes dicen qué regla falló.

`Assert.ThrowsException` es la herramienta del lado del test. En producción, quien espera entrada inválida usa `try`/`catch`:

```csharp
try
{
    var age = Age.Parse(input);
    Console.WriteLine($"Age: {age}");
}
catch (InvalidAgeException ex)
{
    Console.WriteLine(ex.Message);
}
```

Atrapa la excepción específica que puedes manejar. Un `catch (Exception)` en una capa baja esconde errores que no tienen nada que ver con la entrada. El borde superior de un proceso es el lugar para un catch amplio, y aun ahí conviene registrar la excepción antes de seguir.

Cuando la entrada inválida es normal en vez de excepcional, prefiere un método que devuelva un resultado en lugar de lanzar. `int.TryParse` es el ejemplo del framework: un formulario puede llamarlo en cada tecla sin pagar el costo de las excepciones. Nuestro `Parse` está en el borde donde la entrada inválida es una violación del contrato.

## Para cerrar

Lo que cubrimos:

- Un tipo de excepción de dominio que quien llama puede atrapar sin conocer la implementación del parser.
- `int.TryParse` y una variable `out`.
- Cláusulas de guarda para un número inválido y un valor fuera de rango.
- `Assert.ThrowsException` para el camino de error, y `try`/`catch` para quien llama en producción.
- La regla práctica: excepciones para violaciones del contrato, patrón try cuando la entrada inválida es lo esperado.

El capítulo sigue el hábito del camino de error de [Pointers and Errors en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors): el caso inválido es un requisito con su propio test. .NET expresa ese requisito como un tipo de excepción en vez de un valor de error devuelto.

Siguiente: [Propiedades y métodos](../methods/methods.es.md).

