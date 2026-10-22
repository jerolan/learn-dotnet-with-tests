# Decimales

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/decimal)

Un backend que maneja precios, impuestos o facturas tiene que sumar dinero de forma exacta. `double` parece alcanzar, hasta que sumas unos cuantos importes y obtienes un resultado con una cola de dígitos. Empecemos por el problema.

## El problema

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

Este es un test que queremos que falle. Los literales `0.1`, `0.2` y `0.3` son valores `double`, y `double` guarda fracciones binarias. Ninguno de esos tres números tiene una representación binaria exacta, así que cada uno se redondea al valor más cercano que el tipo puede guardar, y la suma arrastra el error.

El error es diminuto. A lo largo de una factura larga, sigue siendo dinero.

## El tipo para el dinero

C# tiene un segundo tipo de punto flotante: `decimal`. Guarda fracciones en base 10, que es la forma en que los precios ya están escritos. El sufijo `m` en un literal lo elige:

```csharp
[TestMethod]
public void AddTwoTenthsWithDecimal()
{
    var total = 0.1m + 0.2m;

    Assert.AreEqual(0.3m, total);
}
```

Ejecútalo. Verde. El único cambio es el sufijo, y la aritmética ahora es exacta.

El conjunto de tests no debería quedarse con un test en rojo, pero el comportamiento de `double` merece quedar fijado para que nadie lo "arregle" cambiando el tipo más adelante. Convierte el primer test en documentación:

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

Los dos pasan, y juntos dicen de qué se trata el capítulo: los mismos dígitos dan resultados distintos según el tipo.

## Un tipo para los importes

Un `decimal` suelto no te impide sumarle una cantidad a un precio ni pasar un importe donde va un conteo. Vamos a darle un nombre al dinero y las operaciones que necesita. La solución terminada está en `fundamentals/decimal`; si estás siguiendo desde cero, arma el proyecto igual que en los capítulos anteriores.

Escribe primero el test:

```csharp
[TestMethod]
public void AddTwoAmounts()
{
    var total = new Money(5.5m).Add(new Money(2.5m));

    Assert.AreEqual(new Money(8.0m), total);
}
```

Ejecuta `dotnet test`. El compilador dice:

```text
error CS0246: The type or namespace name 'Money' could not be found
```

Escribe el tipo más pequeño que compile:

```csharp
namespace Decimal;

public sealed record Money(decimal Amount);
```

Vuelve a ejecutar los tests. El tipo existe, pero `Add` no:

```text
error CS1061: 'Money' does not contain a definition for 'Add'
```

Agrega el método:

```csharp
public sealed record Money(decimal Amount)
{
    public Money Add(Money other) => new(Amount + other.Amount);
}
```

Verde. Un `record` nos da igualdad por valor gratis, y por eso `Assert.AreEqual(new Money(8.0m), total)` compara los importes y no las referencias. `new(Amount + other.Amount)` usa el `new` con tipo inferido que llegó en C# 9; el compilador sabe que el resultado es `Money`.

## Multiplicar un importe por una cantidad

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

## Redondeo

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

Ejecuta los tests. Verde. `MidpointRounding.AwayFromZero` es la regla de redondeo hacia arriba que la mayoría espera cuando dice "redondea a centavos", y ahora se ve en la revisión de código en vez de esconderse en el valor por defecto de una sobrecarga.

## Para cerrar

Lo que cubrimos:

- Por qué `double` no puede representar `0.1`, `0.2` y `0.3` exactamente, y por qué el error importa para el dinero.
- El sufijo `m` y la aritmética con `decimal`.
- Un `record` para un importe, con igualdad por valor y dos operaciones.
- Cantidades enteras multiplicadas por importes decimales.
- `Math.Round` y una política explícita para el punto medio.

El [capítulo Maths de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/maths) arranca desde un problema concreto y deja que los tests dirijan el diseño. Este capítulo hace lo mismo con un problema más pequeño: dinero que tiene que sumar bien.

Siguiente: [Declaraciones, const y var](../declarations.md).

