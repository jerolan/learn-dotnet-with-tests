# Propiedades y métodos

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/methods)

Un envío de 2 kg con una tarifa de 5m por kilogramo debe costar 10m. La entrega exprés suma un recargo de 20m. Necesitamos una calculadora que exprese ambas reglas y permita configurar la tarifa.

La primera llamada no compila porque `ShippingCalculator` todavía no existe:

```csharp
var cost = new ShippingCalculator().Cost(2m);
```

Al ejecutar `dotnet test`, aparece `error CS0246: The type or namespace name 'ShippingCalculator' could not be found`. Usaremos ese error para definir el tipo con un test que falla a la vez. La solución terminada está en `fundamentals/methods`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Methods`, un proyecto de MSTest en `tests/Methods.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

Un método recibe datos y devuelve un valor. Una propiedad guarda datos que quien llama puede configurar. La calculadora expondrá un método con esta forma:

```csharp
public decimal Cost(decimal weightKg)
```

- `public` permite que se use el método desde fuera de la clase.
- `decimal` es el tipo de retorno.
- `Cost` es el nombre del método.
- `(decimal weightKg)` es un parámetro llamado `weightKg` de tipo `decimal`.

Quien llama invoca el método en una instancia de la calculadora, como en `calculator.Cost(2m)`, y recibe un resultado `decimal`.

## Cómo probarlo

Este cálculo no depende de archivos, red ni reloj. Con un peso y una tarifa dados, el resultado siempre es el mismo, así que el test puede llamar al método directamente y comparar su resultado decimal. Usaremos MSTest para crear la calculadora y comprobar el costo esperado.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Methods.Tests;

[TestClass]
public sealed class ShippingCalculatorTests
{
    [TestMethod]
    public void Cost_StandardShipping_ChargesTheRatePerKg()
    {
        var calculator = new ShippingCalculator();

        Assert.AreEqual(10m, calculator.Cost(2m));
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0246: The type or namespace name 'ShippingCalculator' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Methods;

public class ShippingCalculator
{
    public decimal Cost(decimal weightKg)
    {
        return 0;
    }
}
```

El test compila y falla con `Expected:<10>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public decimal Cost(decimal weightKg)
{
    return weightKg * 5m;
}
```

Verde. El test pasa `2m` como argumento, y `Assert.AreEqual` compara el decimal devuelto con `10m`. La tarifa `5m` sigue dentro del método; el siguiente requisito le dará un nombre que quien llama pueda cambiar.

## Refactor

No hay nada que extraer de una sola multiplicación. Mantener el método directo hace que el cálculo se lea mejor. Ejecuta el test otra vez: sigue en verde.

## Repite con requisitos nuevos

### Hacer configurable la tarifa

Escribe el test:

```csharp
[TestMethod]
public void Cost_UsesTheConfiguredRate()
{
    var calculator = new ShippingCalculator { RatePerKg = 8m };

    Assert.AreEqual(16m, calculator.Cost(2m));
}
```

Ejecuta los tests. El compilador dice que `ShippingCalculator` no contiene `RatePerKg`. Agrega una propiedad automática con valor por defecto:

```csharp
public decimal RatePerKg { get; set; } = 5m;
```

Úsala en el método:

```csharp
public decimal Cost(decimal weightKg)
{
    return weightKg * RatePerKg;
}
```

Verde. `{ get; set; }` es una propiedad autoimplementada: el compilador crea el campo de respaldo, el getter y el setter. El inicializador `= 5m` le da una tarifa por defecto a cada calculadora nueva. El test usa un inicializador de objeto, `new ShippingCalculator { RatePerKg = 8m }`, que corre después del constructor y antes de usar la variable.

La propiedad y el método tienen trabajos distintos. La propiedad guarda configuración; el método calcula comportamiento a partir de ella. Mantener la tarifa fuera del método permite que quien llama la cambie sin editar la clase.

### Agregar envío exprés

Escribe el test:

```csharp
[TestMethod]
public void Cost_ExpressShipping_AddsTheSurcharge()
{
    var calculator = new ShippingCalculator();

    Assert.AreEqual(30m, calculator.Cost(2m, express: true));
}
```

Ejecuta los tests. El compilador dice que no hay una sobrecarga de `Cost` que reciba dos argumentos. Agrega un segundo parámetro con valor por defecto:

```csharp
public decimal Cost(decimal weightKg, bool express = false)
{
    var cost = weightKg * RatePerKg;

    return cost;
}
```

El código compila y el test falla con `Expected:<30>. Actual:<10>`. El parámetro está pero no se usa. Agrega el recargo:

```csharp
private const decimal ExpressSurcharge = 20m;

public decimal Cost(decimal weightKg, bool express = false)
{
    var cost = weightKg * RatePerKg;

    if (express)
    {
        cost += ExpressSurcharge;
    }

    return cost;
}
```

Verde, y el test original sigue pasando porque `express` vale `false` por defecto.

`ExpressSurcharge` es privada, así que la constante tiene nombre sin convertirse en configuración para quien llama. `RatePerKg` sigue pública porque quien llama puede elegirla. El método modifica una variable local `cost` y devuelve una sola vez.

La llamada `Cost(2m, express: true)` usa un argumento nombrado. La misma llamada podría escribirse `Cost(2m, true)`, pero el nombre dice qué significa el `true` en el lugar de la llamada. Los argumentos nombrados son más útiles cuando un método tiene varios parámetros del mismo tipo o un booleano que de otro modo sería un misterio.

Un parámetro opcional es una forma de soportar dos formas de llamada. Otra es una sobrecarga: dos métodos con el mismo nombre y distintas listas de parámetros, como los métodos `Divide` del capítulo de tipos. Las sobrecargas son la herramienta correcta cuando cambian los tipos de los parámetros; un valor por defecto es más simple cuando una llamada es solo el caso común de la otra.

## Para cerrar

Lo que cubrimos:

- Definir un método de instancia con parámetros, tipo de retorno y cuerpo.
- Llamar un método desde un test y comparar su valor de retorno.
- Propiedades automáticas con `get`/`set` y un inicializador.
- Inicializadores de objeto para configurar una instancia nueva.
- Parámetros opcionales y argumentos nombrados.
- Una constante privada y la diferencia entre configuración y comportamiento.

El capítulo sigue la sección de métodos de [Structs, Methods and Interfaces en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): un tipo pequeño, un método que calcula algo útil y tests que fijan el resultado. .NET agrega propiedades y argumentos nombrados a la misma idea.

Siguiente: [Clases y objetos](../classes/classes.es.md).

