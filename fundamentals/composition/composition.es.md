# Composición sobre herencia

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/composition)

Quitar los espacios de `"  hello  "` debe producir `"hello"`. Más adelante, el mismo pipeline de texto debe poner el texto en mayúsculas y agregarle un prefijo en el orden de sus pasos.

La primera llamada no compila porque `Trim` no existe:

```csharp
var result = new Trim().Apply("  hello  ");
```

Al ejecutar `dotnet test`, aparece `error CS0246: The type or namespace name 'Trim' could not be found`. Agregaremos una transformación y luego combinaremos varias sin crear una clase por cada combinación. La solución terminada está en `fundamentals/composition`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Composition`, un proyecto de MSTest en `tests/Composition.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

La herencia modela una relación "es un", como un rectángulo que es una figura. La composición modela "usa un": un pipeline usa una lista de transformaciones de texto. Una interfaz `ITextTransform` da a cada paso el mismo método `Apply`, y el pipeline pasa la salida de un paso al siguiente.

## Cómo probarlo

Cada transformación convierte una cadena en otra sin efectos externos. Los tests pueden llamar al pipeline directamente y comprobar su resultado. Un test con varios pasos también verificará que el orden de las transformaciones es parte del comportamiento.

## Escribe primero el test

```csharp
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
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Trim' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Empieza por el contrato y un marcador de posición:

```csharp
namespace Composition;

public interface ITextTransform
{
    string Apply(string input);
}

public sealed class Trim : ITextTransform
{
    public string Apply(string input)
    {
        return input;
    }
}
```

El test falla con `Expected:<hello>. Actual:<  hello  >`.

## Escribe el código suficiente para que pase

```csharp
public string Apply(string input)
{
    return input.Trim();
}
```

Verde. `ITextTransform` es la costura: un contrato pequeño con un método. Cualquier paso que pueda convertir una cadena en otra puede implementarlo.

## Refactor

`Trim.Apply` es una llamada al framework, así que todavía no hay nada que extraer. Ejecuta el test otra vez: sigue en verde. Los siguientes requisitos mostrarán cómo combinar transformaciones independientes.

## Repite con requisitos nuevos

### Ejecutar varias transformaciones

Escribe el test:

```csharp
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
```

El compilador se queja de `TextPipeline`, `UpperCase` y `Prefix`. Agrega las transformaciones:

```csharp
public sealed class UpperCase : ITextTransform
{
    public string Apply(string input)
    {
        return input.ToUpperInvariant();
    }
}

public sealed class Prefix : ITextTransform
{
    private readonly string _prefix;

    public Prefix(string prefix)
    {
        _prefix = prefix;
    }

    public string Apply(string input)
    {
        return _prefix + input;
    }
}
```

Después agrega el pipeline:

```csharp
public sealed class TextPipeline
{
    private readonly IEnumerable<ITextTransform> _steps;

    public TextPipeline(IEnumerable<ITextTransform> steps)
    {
        _steps = steps;
    }

    public string Apply(string input)
    {
        var result = input;

        foreach (var step in _steps)
        {
            result = step.Apply(result);
        }

        return result;
    }
}
```

Verde. El pipeline no hereda de `Trim` ni de `UpperCase`; guarda una lista de pasos y llama a cada uno. El orden de la lista es el orden de las transformaciones.

### El orden importa

Escribe el test:

```csharp
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
```

Pasa. El primer pipeline puso `hello` en mayúsculas y después agregó el prefijo en minúsculas; este agrega el prefijo primero, así que la transformación de mayúsculas también cambia el prefijo. El pipeline no decide el orden; lo decide quien llama.

### Sin pasos

Escribe el test de borde:

```csharp
[TestMethod]
public void Apply_WithNoSteps_ReturnsTheInput()
{
    var pipeline = new TextPipeline(Array.Empty<ITextTransform>());

    Assert.AreEqual("hello", pipeline.Apply("hello"));
}
```

Pasa. Una lista vacía significa que el bucle nunca corre y la entrada vuelve sin cambios. Es un valor por defecto útil para un pipeline armado desde configuración.

### Refactorizar el pipeline

El código ya es pequeño. La lección de diseño es lo que no se creó. Un diseño con herencia necesitaría una clase por cada combinación:

```csharp
public sealed class TrimmedUpperCase : ITextTransform { ... }
public sealed class TrimmedUpperCaseWithPrefix : ITextTransform { ... }
public sealed class TrimmedWithPrefixAndUpperCase : ITextTransform { ... }
```

Cada transformación nueva multiplica la cantidad de clases, y cada clase repite el mismo bucle. La composición reemplaza ese conjunto por un pipeline y una lista de pasos independientes.

La costura es `ITextTransform`. Agregar una transformación `ReplaceSpaces` o `Truncate` significa escribir una clase y pasarla; `TextPipeline` no cambia. Esa es la idea de abierto/cerrado en la práctica: extiendes agregando un paso, no editando el pipeline.

El mismo constructor prepara la inyección de dependencias. `TextPipeline` no crea sus pasos; los recibe. El siguiente capítulo mira quién los crea y cómo un contenedor los conecta.

## Para cerrar

Lo que cubrimos:

- Composición como "usa un" en vez de herencia como "es un".
- Una interfaz pequeña como costura entre el pipeline y sus pasos.
- Un pipeline que ejecuta una lista ordenada de transformaciones.
- El orden como parte del comportamiento, no un detalle de implementación.
- Una lista vacía como borde válido.
- Por qué la composición evita la explosión de clases de una jerarquía de combinaciones.

El capítulo se apoya en la sección de interfaces de [Structs, Methods and Interfaces en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): un contrato pequeño deja que piezas independientes encajen. El pipeline es la versión por composición de la jerarquía de figuras del capítulo anterior.

Siguiente: [Colecciones](../collections/collections.es.md).

