# Control de flujo

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/control-flow)

Repetir una cadena una vez debe devolverla sin cambios; repetirla más veces requiere un bucle. Las mismas herramientas de control de flujo nos permiten agregar una suma y FizzBuzz en pasos pequeños y comprobables.

La solución terminada está en `fundamentals/control-flow`. Si la estás armando desde cero, prepárala como las demás: una biblioteca de clases en `src/ControlFlow`, un proyecto de MSTest en `tests/ControlFlow.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Los bucles repiten trabajo; las condiciones eligen qué rama se ejecuta. Un bucle `for` sirve para una cantidad conocida, un `while` comprueba una condición e `if` o `switch` elige un resultado. Cada ejemplo devuelve un valor directamente, así que los tests pueden centrarse en la regla que se está probando.

## Cómo probarlo

Llama a estos métodos directamente desde MSTest y comprueba sus valores de retorno. Usa una tabla de casos para FizzBuzz y tests de borde separados para cero repeticiones y un límite de suma igual a cero.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ControlFlow.Tests;

[TestClass]
public sealed class ControlFlowExamplesTests
{
    [TestMethod]
    public void RepeatOnce_ReturnsTheText()
    {
        Assert.AreEqual("a", ControlFlowExamples.Repeat("a"));
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0103: The name 'ControlFlowExamples' does not exist in the current context
```

## Escribe el código mínimo para que el test compile y mira la salida

Crea la clase con una implementación provisional:

```csharp
namespace ControlFlow;

public static class ControlFlowExamples
{
    public static string Repeat(string text)
    {
        return string.Empty;
    }
}
```

El test compila y falla con `Expected:<a>. Actual:<>`.

## Escribe el código suficiente para que pase

Devuelve el texto sin cambios:

```csharp
public static string Repeat(string text)
{
    return text;
}
```

Ejecuta los tests. Verde. Esta implementación mínima también satisface por completo el primer requisito.

## Refactor

No hay lógica repetida que extraer de un método que devuelve su argumento. Déjalo como está; el siguiente requisito mostrará cuándo sirve un bucle.

## Repite con requisitos nuevos

### Repetir el texto varias veces

El siguiente requisito es el que necesita un bucle. Escribe el test:

```csharp
[TestMethod]
public void Repeat_ReturnsTheTextRepeated()
{
    Assert.AreEqual("aaa", ControlFlowExamples.Repeat("a", 3));
}
```

Ejecuta los tests. El compilador dice:

```text
error CS1501: No overload for method 'Repeat' takes 2 arguments
```

Cambia la firma para que reciba una cantidad. Deja el cuerpo anterior un momento para que el código compile:

```csharp
public static string Repeat(string text, int times)
{
    return text;
}
```

El test anterior ya no compila porque `Repeat` no tiene una forma de un solo argumento. Ese test describía la forma previa del método; reemplázalo por el test de dos argumentos. Ejecuta la suite y el test nuevo falla con `Expected:<aaa>. Actual:<a>`.

Ahora escribe el bucle:

```csharp
using System.Text;

namespace ControlFlow;

public static class ControlFlowExamples
{
    public static string Repeat(string text, int times)
    {
        var result = new StringBuilder();

        for (var i = 0; i < times; i++)
        {
            result.Append(text);
        }

        return result.ToString();
    }
}
```

Verde. El bucle `for` tiene tres partes:

- `var i = 0` corre una vez, antes del bucle.
- `i < times` se comprueba antes de cada iteración.
- `i++` corre después de cada iteración.

`StringBuilder` acumula las piezas sin asignar una cadena nueva en cada append. Para tres caracteres una concatenación normal también funcionaría, pero el builder es el hábito que quieres cuando la cantidad viene de la entrada del usuario.

### Un borde: cero repeticiones

Escribe un test para el caso límite:

```csharp
[TestMethod]
public void Repeat_ZeroTimes_ReturnsEmpty()
{
    Assert.AreEqual(string.Empty, ControlFlowExamples.Repeat("a", 0));
}
```

Pasa sin cambiar el código, porque `i < 0` es falso en la primera comprobación y el cuerpo del bucle nunca corre. Un test que pasa de inmediato igual sirve: fija el borde para que un refactor posterior no lo rompa en silencio.

### Sumar con un bucle while

Escribe el test:

```csharp
[TestMethod]
public void SumTo_AddsTheNumbersUpToTheLimit()
{
    Assert.AreEqual(10, ControlFlowExamples.SumTo(4));
}
```

El compilador dice que falta `SumTo`. Agrégalo con un marcador de posición:

```csharp
public static int SumTo(int limit)
{
    return 0;
}
```

El test falla con `Expected:<10>. Actual:<0>`. Ahora escribe el bucle:

```csharp
public static int SumTo(int limit)
{
    var sum = 0;
    var current = 1;

    while (current <= limit)
    {
        sum += current;
        current++;
    }

    return sum;
}
```

Verde. Un bucle `for` también funcionaría aquí, porque la cantidad de iteraciones se conoce. La forma `while` separa la condición del incremento, y se lee mejor cuando la regla de corte depende de los datos en vez de un contador.

Agrega el borde:

```csharp
[TestMethod]
public void SumTo_Zero_ReturnsZero()
{
    Assert.AreEqual(0, ControlFlowExamples.SumTo(0));
}
```

Pasa. La condición del bucle es falsa antes de la primera iteración, así que el método devuelve `0`.

### FizzBuzz con condiciones

FizzBuzz es la prueba clásica del flujo condicional: los múltiplos de tres dicen `Fizz`, los de cinco dicen `Buzz`, los de ambos dicen `FizzBuzz`, y el resto dice el número.

Escribe primero la tabla:

```csharp
[DataTestMethod]
[DataRow(1, "1")]
[DataRow(3, "Fizz")]
[DataRow(5, "Buzz")]
[DataRow(15, "FizzBuzz")]
public void FizzBuzz_ReturnsTheRightWord(int number, string want)
{
    Assert.AreEqual(want, ControlFlowExamples.FizzBuzz(number));
}
```

Agrega el método con un marcador de posición:

```csharp
public static string FizzBuzz(int number)
{
    return string.Empty;
}
```

La primera fila falla: `Expected:<1>. Actual:<>`. Escribe las condiciones:

```csharp
public static string FizzBuzz(int number)
{
    if (number % 15 == 0)
    {
        return "FizzBuzz";
    }

    if (number % 3 == 0)
    {
        return "Fizz";
    }

    if (number % 5 == 0)
    {
        return "Buzz";
    }

    return number.ToString();
}
```

Verde. El orden importa. `15` es divisible por `3` y por `5`, así que la comprobación de `FizzBuzz` tiene que ir primero; si no, la rama de `Fizz` volvería antes. El operador `%` da el resto, y `number % 3 == 0` es verdadero para los múltiplos de tres.

### Refactorizar la decisión de FizzBuzz

Las tres sentencias `if` prueban el mismo par de restos. C# puede hacer pattern matching sobre una tupla en una expresión switch:

```csharp
public static string FizzBuzz(int number)
{
    return (number % 3, number % 5) switch
    {
        (0, 0) => "FizzBuzz",
        (0, _) => "Fizz",
        (_, 0) => "Buzz",
        _ => number.ToString()
    };
}
```

Ejecuta los tests. Verde, y los cuatro casos se ven a la vez. El `_` es un descarte: coincide con cualquier valor y dice que no lo usamos. La expresión switch devuelve un valor, así que no hay `break` ni fall-through.

## Para cerrar

Lo que cubrimos:

- Un bucle `for` con inicializador, condición e incremento.
- Un bucle `while` cuya condición controla las iteraciones.
- Tests de borde para cero repeticiones y un límite cero.
- Cadenas de `if` y el operador `%`.
- Una expresión switch con patrones de tupla y descartes.

El capítulo sigue el [capítulo Iteration de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/iteration): repetir un texto, dejar que la cantidad llegue como un requisito nuevo y mantener el bucle honesto con un test de borde. FizzBuzz y la expresión switch son los agregados de .NET.

Siguiente: [Excepciones](../exceptions/exceptions.es.md).

