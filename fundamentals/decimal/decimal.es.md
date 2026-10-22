# Decimales

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/decimal)

El total de una factura debería sumar dos décimas y dar exactamente tres décimas. Con `double`, no pasa:

Escribe un test que sume dos décimas:

```csharp
[TestMethod]
public void AddTwoTenths()
{
    var total = 0.1 + 0.2;

    Assert.AreEqual(0.3, total);
}
```

Ejecútalo:

```text
  Failed AddTwoTenths [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<0.3>. Actual:<0.30000000000000004>.
```

## Cómo funciona

Los literales `0.1`, `0.2` y `0.3` son valores `double`, y `double` guarda fracciones binarias. Ninguno de esos números tiene una representación binaria exacta, así que cada uno se redondea al valor más cercano que el tipo puede guardar y la suma arrastra el error. El error es diminuto; a lo largo de una factura larga, sigue siendo dinero.

C# también tiene `decimal`, que guarda un entero en base 10 y una escala decimal. El sufijo `m` lo elige, así que estas décimas se pueden representar exactamente:

```csharp
[TestMethod]
public void AddTwoTenthsWithDecimal()
{
    var total = 0.1m + 0.2m;

    Assert.AreEqual(0.3m, total);
}
```

Ejecútalo. Verde. Solo cambia el sufijo, y la aritmética ahora es exacta.

## Cómo probarlo

Estos ejemplos de aritmética no necesitan E/S ni esperas. Después de confirmar el fallo anterior, conserva tests en verde que fijen la diferencia entre `double` y `decimal`:

```csharp
[TestMethod]
public void DoubleTenthsAreNotExact()
{
    Assert.AreNotEqual(0.3, 0.1 + 0.2);
}

[TestMethod]
public void DecimalTenthsAreExact()
{
    Assert.AreEqual(0.3m, 0.1m + 0.2m);
}
```

Los dos pasan, y juntos dicen de qué se trata el capítulo: los mismos dígitos dan resultados distintos según el tipo. La suite no debe dejar el test fallido; reemplázalo por la aserción `DoubleTenthsAreNotExact`.

## Escribe primero el test

Un `decimal` suelto no te impide sumarle una cantidad a un precio ni pasar un importe donde va un conteo. Dale un nombre al dinero y las operaciones que necesita. La solución terminada está en `fundamentals/decimal`; si estás siguiendo desde cero, arma el proyecto igual que en los capítulos anteriores.

```csharp
[TestMethod]
public void AddTwoAmounts()
{
    var total = new Money(5.5m).Add(new Money(2.5m));

    Assert.AreEqual(new Money(8.0m), total);
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`. El compilador dice:

```text
error CS0246: The type or namespace name 'Money' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Escribe el tipo más pequeño:

```csharp
namespace Decimal;

public sealed record Money(decimal Amount);
```

El test todavía no compila porque falta `Add`:

```text
error CS1061: 'Money' does not contain a definition for 'Add'
```

## Escribe el código suficiente para que pase

Agrega el método:

```csharp
public sealed record Money(decimal Amount)
{
    public Money Add(Money other) => new(Amount + other.Amount);
}
```

Ejecuta los tests. Verde. Un `record` nos da igualdad por valor gratis, y por eso `Assert.AreEqual(new Money(8.0m), total)` compara los importes y no las referencias. `new(Amount + other.Amount)` usa el `new` con tipo inferido que llegó en C# 9; el compilador sabe que el resultado es `Money`.

## Refactor

El record inmutable y su método `Add` ya son pequeños y directos. Ejecuta el test otra vez; sigue en verde. No hay nada que extraer, así que agrega la siguiente operación como un requisito aparte.

## Repite con requisitos nuevos

### Multiplicar un importe por una cantidad

Escribe el siguiente test:

```csharp
[TestMethod]
public void MultiplyPriceByQuantity()
{
    var total = new Money(19.95m).Multiply(3);

    Assert.AreEqual(new Money(59.85m), total);
}
```

El compilador dice que falta `Multiply`. Agrégalo:

```csharp
public Money Multiply(int quantity) => new(Amount * quantity);
```

Verde. La cantidad es `int` porque compras una cantidad entera de artículos; el importe sigue siendo `decimal` porque el dinero es decimal.

### Redondeo

El dinero tiene una decisión más que el tipo no puede tomar por ti: qué hacer con un punto medio. Escribe un test que fuerce la pregunta:

```csharp
[TestMethod]
public void RoundingAwayFromZero()
{
    var rounded = new Money(10.545m).Rounded(2);

    Assert.AreEqual(new Money(10.55m), rounded);
}
```

Agrega el método con la implementación obvia:

```csharp
public Money Rounded(int places = 2) => new(Math.Round(Amount, places));
```

Ejecuta el test. Falla:

```text
  Failed RoundingAwayFromZero [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<10.55>. Actual:<10.54>.
```

El valor por defecto de `Math.Round` es el redondeo bancario, también llamado mitad a par. El dígito anterior al punto medio es `4`, que es par, así que el valor se queda en `10.54`. Es un valor razonable para estadística y sorprendente para una factura.

Haz explícita la política:

```csharp
public Money Rounded(int places = 2) =>
    new(Math.Round(Amount, places, MidpointRounding.AwayFromZero));
```

Ejecuta los tests. Verde. `MidpointRounding.AwayFromZero` hace explícita la regla para los puntos medios: para este importe positivo, `10.545` pasa a `10.55`. La política se ve en la revisión de código en vez de esconderse en el valor por defecto de una sobrecarga.

## Para cerrar

Lo que cubrimos:

- Por qué `double` no puede representar `0.1`, `0.2` y `0.3` exactamente, y por qué el error importa para el dinero.
- El sufijo `m` y la aritmética con `decimal`.
- Un `record` para un importe, con igualdad por valor y dos operaciones.
- Cantidades enteras multiplicadas por importes decimales.
- `Math.Round` y una política explícita para el punto medio.

El [capítulo Maths de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/maths) arranca desde un problema concreto y deja que los tests dirijan el diseño. Este capítulo hace lo mismo con un problema más pequeño: dinero que tiene que sumar bien.

Siguiente: [Declaraciones, const y var](../declarations/declarations.es.md).

