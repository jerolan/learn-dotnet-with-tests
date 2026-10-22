# Declaraciones, const y var

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/declarations)

Una compra de 100m con 16 por ciento de IVA debe sumar 116m. Multiplicar por `1.16m` da ese resultado, pero esconde la tasa dentro del cálculo.

La primera llamada no compila porque `PriceCalculator` no existe:

```csharp
var calculator = new PriceCalculator();
var total = calculator.TotalWithVat(100m);
```

Al ejecutar `dotnet test`, aparece `error CS0246: The type or namespace name 'PriceCalculator' could not be found`. Daremos nombre a la tasa y decidiremos si pertenece al tipo o a cada calculadora. La solución terminada está en `fundamentals/declarations`; si la estás armando desde cero, prepárala como en los capítulos anteriores: una biblioteca de clases en `src/Declarations`, un proyecto de MSTest en `tests/Declarations.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

En `var calculator = new PriceCalculator()`, el compilador infiere que la variable local es de tipo `PriceCalculator`. El tipo queda fijo en compilación. `TotalWithVat` recibirá un precio `decimal` y devolverá un total `decimal`; `decimal` mantiene este cálculo de dinero en aritmética decimal.

## Cómo probarlo

La calculadora solo transforma un valor en otro. No lee archivos, llama a servicios ni depende de la hora actual, así que el test puede llamar a `TotalWithVat` y comparar directamente el importe devuelto.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Declarations.Tests;

[TestClass]
public sealed class PriceCalculatorTests
{
    [TestMethod]
    public void TotalWithVat_AddsTheDefaultRate()
    {
        var calculator = new PriceCalculator();

        Assert.AreEqual(116m, calculator.TotalWithVat(100m));
    }
}
```

El sufijo `m` hace que `100m` y `116m` sean valores `decimal`. El dinero sigue siendo decimal; el capítulo anterior explicó por qué.

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0246: The type or namespace name 'PriceCalculator' could not be found
```

El test nombra un tipo que no existe. Ese es el primer rojo.

## Escribe el código mínimo para que el test compile y mira la salida

Crea el tipo con un método que compile:

```csharp
namespace Declarations;

public sealed class PriceCalculator
{
    public decimal TotalWithVat(decimal price)
    {
        return 0;
    }
}
```

Ejecuta los tests. El código compila y el test falla con `Expected:<116>. Actual:<0>`.

## Escribe el código suficiente para que pase

Escribe el cálculo directamente:

```csharp
public decimal TotalWithVat(decimal price)
{
    return price * 1.16m;
}
```

Verde. El literal `1.16m` es el precio más el 16 por ciento, expresado como un solo número. Funciona, pero esconde la tasa dentro de la expresión.

## Refactor

Extrae la tasa a una constante:

```csharp
public const decimal DefaultVatRate = 0.16m;

public decimal TotalWithVat(decimal price)
{
    var vat = price * DefaultVatRate;
    return price + vat;
}
```

Ejecuta los tests. Verde, y ahora el código dice qué significaba `1.16m`.

Una `const` es una constante de compilación. El compilador sustituye el valor donde se usa, y por eso tiene que ser un literal u otra constante. Las constantes son implícitamente estáticas: pertenecen al tipo, no a una instancia.

La palabra `var` es otra cosa. Es inferencia de tipo local: el compilador mira el lado derecho y le da ese tipo a la variable local. Aquí `vat` es `decimal` porque `price * DefaultVatRate` es decimal. El tipo queda fijo en compilación; `var` no es un tipo dinámico.

Eso también significa que `var` es solo para variables locales. No puedes declarar un campo ni un parámetro con él, y no puedes cambiar el tipo después:

```csharp
var total = 0;      // int
total = 1.5m;       // error CS0266: Cannot implicitly convert type decimal to int
```

Usa `var` cuando el lado derecho deja claro el tipo, como en `var vat = price * DefaultVatRate`. Escribe el tipo cuando el lector tendría que adivinarlo, sobre todo con resultados de métodos cuyo nombre no anuncia el tipo.

## Repite con requisitos nuevos

Una calculadora real necesita más de una tasa: algunos productos pagan 16 por ciento, otros 8, otros ninguno. La tasa pertenece a la calculadora, no al tipo.

Escribe el test:

```csharp
[TestMethod]
public void TotalWithVat_UsesTheRatePassedToTheConstructor()
{
    var calculator = new PriceCalculator(0.08m);

    Assert.AreEqual(108m, calculator.TotalWithVat(100m));
}
```

Ejecuta los tests. El compilador dice:

```text
error CS1729: 'PriceCalculator' does not contain a constructor that takes 1 arguments
```

Agrega un campo que cada calculadora pueda fijar una sola vez:

```csharp
public sealed class PriceCalculator
{
    public const decimal DefaultVatRate = 0.16m;

    private readonly decimal _vatRate;

    public PriceCalculator()
        : this(DefaultVatRate)
    {
    }

    public PriceCalculator(decimal vatRate)
    {
        _vatRate = vatRate;
    }

    public decimal TotalWithVat(decimal price)
    {
        var vat = price * _vatRate;
        return price + vat;
    }
}
```

Ejecuta los tests. Pasan los dos.

`readonly` significa que el campo se puede asignar en el constructor y en ningún otro lugar. Después de construido se comporta como una constante para ese objeto, pero cada objeto puede guardar un valor distinto. Esa es la diferencia con `const`: `const` es un valor para todo el tipo, fijado en compilación; `readonly` es un valor por instancia, fijado cuando se crea la instancia.

Intenta asignar `_vatRate` desde `TotalWithVat` y el compilador te detiene:

```text
error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
```

## Para cerrar

Lo que cubrimos:

- Tipos explícitos y variables locales inferidas con `var`.
- `var` es solo local y el tipo inferido no cambia.
- `const` para un valor fijo en compilación y compartido por el tipo.
- `readonly` para un valor fijo por instancia en el constructor.
- Una calculadora cuya tasa por defecto es una constante y cuya tasa real es un campo readonly.

Siguiente: [Control de flujo](../control-flow/control-flow.es.md), donde la calculadora empieza a tomar decisiones y repetir trabajo.

