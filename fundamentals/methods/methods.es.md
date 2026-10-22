# Propiedades y métodos

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/methods)

Un método es comportamiento pegado a un tipo: recibe parámetros, hace algo y normalmente devuelve un valor. Una propiedad es dato con accesores. Juntos forman la superficie de una clase, y sus nombres y firmas deciden cómo se lee el tipo en el lugar de la llamada.

Vamos a construir una calculadora de envíos. Cobra una tarifa por kilogramo y suma un recargo por entrega exprés. La solución terminada está en `fundamentals/methods`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Methods`, un proyecto de MSTest en `tests/Methods.Tests`, una solución y una referencia entre proyectos.

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

Verde. Lee la firma del método de izquierda a derecha:

- `public` es el modificador de acceso: quien llama desde fuera de la clase puede usarlo.
- `decimal` es el tipo de retorno.
- `Cost` es el nombre.
- `(decimal weightKg)` es la lista de parámetros: un parámetro llamado `weightKg` de tipo `decimal`.

La llamada `calculator.Cost(2m)` pasa `2m` como argumento. El método devuelve un `decimal`, y `Assert.AreEqual` compara ese valor con `10m`.

La tarifa `5m` es un número mágico dentro del método. El siguiente requisito le da un nombre que quien llama puede cambiar.

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

La llamada `Cost(2m, express: true)` usa un argumento nombrado. La misma llamada podría escribirse `Cost(2m, true)`, pero el nombre dice qué significa el `true` en el lugar de la llamada. Los argumentos nombrados son más útiles cuando un método tiene varios parámetros del mismo tipo o un booleano que de otro modo sería un misterio.

Un parámetro opcional es una forma de soportar dos formas de llamada. Otra es una sobrecarga: dos métodos con el mismo nombre y distintas listas de parámetros, como los métodos `Divide` del capítulo de tipos. Las sobrecargas son la herramienta correcta cuando cambian los tipos de los parámetros; un valor por defecto es más simple cuando una llamada es solo el caso común de la otra.

## Refactor

El código de producción es pequeño. El refactor aquí es hacer que las piezas se lean como una unidad:

- `ExpressSurcharge` es una constante privada, así el número tiene nombre y quien llama no puede cambiarlo.
- `RatePerKg` es pública porque es configuración.
- El método calcula en una variable local y devuelve una sola vez, lo que mantiene la rama fácil de seguir.

Ejecuta los tests después de la extracción. Verde, y la superficie pública ahora es una propiedad y un método.

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

