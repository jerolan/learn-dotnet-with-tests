# Datos de test con Bogus

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/test-data-bogus)

Un validador de pedidos revisa un email y una fecha. Probar el camino feliz necesita cien pedidos válidos: nombres, emails, totales, timestamps. Escribirlos a mano convierte el test en un ejercicio de captura de datos, y cuando falla tienes que averiguar si el problema está en el validador o en el fixture.

Bogus construye esos datos a partir de reglas. Este apéndice lo usa para el grueso de los casos válidos y deja valores explícitos para los bordes.

## Cómo funciona

Bogus construye cada pedido aplicando reglas a nombres, emails, totales y fechas generados. `UseSeed` hace reproducibles las decisiones aleatorias; una ventana de fechas fija también controla la parte que depende del reloj.

## Cómo probarlo

Usa datos generados para un conjunto amplio de pedidos válidos y valores literales para cada caso inválido. Mantén Bogus en el proyecto de tests para que el validador de producción no dependa de una librería de fixtures.

### Prepara la solución

Desde la carpeta del apéndice:

```sh
dotnet new classlib -n Shop -o src/Shop -f net6.0
dotnet new mstest -n Shop.Tests -o tests/Shop.Tests -f net6.0
dotnet new sln -n TestDataBogus
dotnet sln TestDataBogus.sln add src/Shop/Shop.csproj tests/Shop.Tests/Shop.Tests.csproj
dotnet add tests/Shop.Tests/Shop.Tests.csproj reference src/Shop/Shop.csproj
```

Borra el `Class1.cs` y el `UnitTest1.cs` que genera la plantilla. El proyecto de tests es el único que referenciará Bogus; el proyecto de producción no lo necesita.

## Escribe primero el test

Crea `tests/Shop.Tests/OrderValidatorTests.cs`:

```csharp
using Bogus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shop;

namespace Shop.Tests;

[TestClass]
public sealed class OrderValidatorTests
{
    [TestMethod]
    public void ValidGeneratedOrdersPass()
    {
        var orders = new Faker<Order>()
            .UseSeed(1234)
            .CustomInstantiator(f => new Order(
                f.Name.FullName(),
                f.Internet.Email(),
                f.Random.Decimal(1m, 500m),
                f.Date.Between(
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))))
            .Generate(100);

        var validator = new OrderValidator();

        foreach (var order in orders)
        {
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                validator.Validate(order).ToArray(),
                order.ToString());
        }
    }
}
```

Las reglas describen los datos válidos una vez. `Generate(100)` las aplica cien veces y el loop pasa cada resultado por la misma aserción.

## Intenta ejecutar el test

Ejecuta `dotnet test`. El compilador todavía no encuentra Bogus:

```text
error CS0246: The type or namespace name 'Bogus' could not be found (are you missing a using directive or an assembly reference?)
```

## Escribe el código mínimo para que el test compile y mira la salida

Bogus solo pertenece al proyecto de tests, así que agrégalo y fija la versión:

```sh
dotnet add tests/Shop.Tests/Shop.Tests.csproj package Bogus --version 34.0.1
```

Ejecuta de nuevo. El generador ya está disponible; el compilador ahora pide los tipos de producción que describe el test:

```text
error CS0246: The type or namespace name 'Order' could not be found (are you missing a using directive or an assembly reference?)
error CS0246: The type or namespace name 'OrderValidator' could not be found (are you missing a using directive or an assembly reference?)
```

## Escribe el código suficiente para que pase

`src/Shop/Order.cs`:

```csharp
namespace Shop;

public sealed record Order(
    string CustomerName,
    string Email,
    decimal Total,
    DateTime PlacedAt);
```

`src/Shop/OrderValidator.cs`:

```csharp
namespace Shop;

public sealed class OrderValidator
{
    public IReadOnlyList<string> Validate(Order order)
    {
        return Array.Empty<string>();
    }
}
```

Corre `dotnet test`. Verde, con un test. Desconfía de ese verde: el validador todavía no rechaza nada, así que el test solo demuestra que los datos generados llegan al loop.

### Por qué CustomInstantiator

Bogus suele crear un objeto con su constructor sin parámetros y después llena las propiedades escribibles que nombra `RuleFor`. Un record posicional no tiene ninguna de las dos cosas. Si escribes el test con `RuleFor`:

```text
System.MissingMethodException: Cannot dynamically create an instance of type 'Shop.Order'.
Reason: No parameterless constructor defined.
```

`CustomInstantiator` le entrega el faker a una fábrica que escribes tú, así el record conserva su constructor y cada campo se asigna una vez. Para una clase con propiedades escribibles, `RuleFor` sigue siendo la forma corta.

## Refactor

El primer test tiene un escenario de datos válidos generados. Todavía no hace falta extraer un helper; mantener las reglas junto a la aserción deja visible cómo se construye el fixture. Cuando otro test necesite los mismos datos, el requisito de la semilla de abajo da motivo para moverlos a un helper.

### Cuándo usar Bogus

- Genera el grueso de los datos válidos de un camino. Deja explícitos los bordes y los mensajes esperados.
- Fija la semilla de todo faker que uses en un test. Un fallo que no se puede reproducir es un rumor.
- Fija todo lo demás que la semilla no controla: fechas, identificadores, locale y estado externo.
- No generes el valor esperado con las mismas reglas que generan la entrada. El generador no es un oráculo.
- Un valor generado dentro de una aserción solo es seguro mientras la semilla y la versión de la librería estén fijas.

## Repite con requisitos nuevos

La primera regla: un pedido cuyo email no tiene `@` es inválido. Es un borde, así que dale un valor literal en vez de generarlo:

```csharp
[TestMethod]
public void OrderWithoutEmailIsRejected()
{
    var order = new Order(
        "Ana Torres",
        "ana.example.com",
        12.50m,
        DateTime.UtcNow.AddMinutes(-1));

    var errors = new OrderValidator().Validate(order);

    CollectionAssert.Contains(errors.ToList(), "Email is invalid.");
}
```

Corre. La aserción falla porque `Validate` devuelve una lista vacía.

Agrega la comprobación:

```csharp
var errors = new List<string>();

if (!order.Email.Contains('@'))
{
    errors.Add("Email is invalid.");
}

return errors;
```

Verde. El test generado sigue pasando, que es la gracia de mantener el fixture válido amplio: una regla nueva no debería romper los datos válidos.

La siguiente regla: un pedido no puede hacerse en el futuro. Escribe el test con una fecha un día adelante:

```csharp
[TestMethod]
public void OrderPlacedInTheFutureIsRejected()
{
    var order = new Order(
        "Ana Torres",
        "ana@example.com",
        12.50m,
        DateTime.UtcNow.AddDays(1));

    var errors = new OrderValidator().Validate(order);

    CollectionAssert.Contains(errors.ToList(), "Placed date cannot be in the future.");
}
```

Rojo. Agrega la comparación:

```csharp
if (order.PlacedAt > DateTime.UtcNow)
{
    errors.Add("Placed date cannot be in the future.");
}
```

Verde.

### La semilla no congela el reloj

Un fixture generado solo sirve si la misma semilla produce los mismos datos. Mueve la generación a un helper para que las dos llamadas usen las mismas reglas:

```csharp
private static List<Order> ValidOrders(int seed)
{
    return new Faker<Order>()
        .UseSeed(seed)
        .CustomInstantiator(f => new Order(
            f.Name.FullName(),
            f.Internet.Email(),
            f.Random.Decimal(1m, 500m),
            f.Date.Past(1).ToUniversalTime()))
        .Generate(100);
}
```

Ahora escribe la garantía como test:

```csharp
[TestMethod]
public void SameSeedProducesTheSameOrders()
{
    var first = ValidOrders(seed: 1234);
    var second = ValidOrders(seed: 1234);

    CollectionAssert.AreEqual(first, second);
}
```

Sigue fallando:

```text
CollectionAssert.AreEqual failed. (Element at index 0 do not match.)
```

La semilla controla las elecciones aleatorias. No controla `f.Date.Past(1)`, que lee la hora actual cada vez que corre el faker. Dos llamadas separadas por unos milisegundos producen timestamps distintos.

Reemplaza la regla de fecha del helper por una ventana fija:

```csharp
f.Date.Between(
    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))
```

Corre otra vez. Verde. Si el validador comparara contra un reloj que no puedes fijar, inyectarías ese reloj en vez de leer `DateTime.UtcNow` dentro del validador.

### Verificación

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 98 ms
```

Los cuatro tests cubren cien pedidos válidos generados, un email inválido, una fecha futura y la garantía de la semilla.

## Para cerrar

Lo que cubrimos:

- Agregar Bogus a un proyecto de tests y fijar su versión.
- Construir un record posicional con `CustomInstantiator`.
- Usar datos generados para el camino feliz y valores literales para los bordes.
- Una semilla que reproduce las elecciones aleatorias y una ventana fija para los timestamps que no controla.
- Un helper que mantiene las reglas de generación en un solo lugar.

Sigue: CI/CD.
