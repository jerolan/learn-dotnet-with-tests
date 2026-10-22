# Clases y objetos

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/classes)

Una clase es un plano. Un objeto es una instancia de ese plano con su propio estado. La clase dice qué datos guarda el objeto y qué operaciones soporta; el objeto guarda los valores.

Vamos a construir una cuenta bancaria. Se abre con un saldo, acepta depósitos y retiros, y se niega a bajar de cero. La solución terminada está en `fundamentals/classes`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Classes`, un proyecto de MSTest en `tests/Classes.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Classes.Tests;

[TestClass]
public sealed class BankAccountTests
{
    [TestMethod]
    public void NewAccount_HasTheInitialBalance()
    {
        var account = new BankAccount(100m);

        Assert.AreEqual(100m, account.Balance);
    }
}
```

## Intenta ejecutar el test

Ejecuta `dotnet test`:

```text
error CS0246: The type or namespace name 'BankAccount' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Classes;

public class BankAccount
{
    public BankAccount(decimal initialBalance)
    {
    }

    public decimal Balance { get; set; }
}
```

El código compila y el test falla con `Expected:<100>. Actual:<0>`. El constructor corre, pero ignora su parámetro.

## Escribe el código suficiente para que pase

```csharp
public BankAccount(decimal initialBalance)
{
    Balance = initialBalance;
}
```

Verde. `new BankAccount(100m)` crea un objeto y ejecuta el constructor con `100m` como argumento. El objeto guarda ese valor en `Balance`; una segunda llamada a `new` crearía otro objeto con su propio saldo.

## Refactor

El test pasa, pero `Balance` tiene un setter público: cualquiera puede escribir `account.Balance = 1_000_000m` y saltearse la clase entera. La clase debería ser dueña de su estado.

Cambia la propiedad a `{ get; private set; }`:

```csharp
public decimal Balance { get; private set; }
```

Ejecuta los tests. Siguen en verde, porque el constructor puede usar el setter privado. Desde fuera de la clase, la propiedad es de solo lectura:

```csharp
account.Balance = 200m;
// error CS0272: The property or indexer 'BankAccount.Balance' cannot be used
// in this context because the set accessor is inaccessible
```

Eso es encapsulación en una línea: quien llama puede leer el saldo, y solo la clase puede cambiarlo.

## Repite con requisitos nuevos

### Depósito

Escribe el test:

```csharp
[TestMethod]
public void Deposit_AddsToTheBalance()
{
    var account = new BankAccount(100m);

    account.Deposit(50m);

    Assert.AreEqual(150m, account.Balance);
}
```

El compilador dice que `Deposit` no existe. Agrega el método:

```csharp
public void Deposit(decimal amount)
{
    Balance += amount;
}
```

Verde. `void` significa que el método no devuelve nada; su efecto es el cambio que hace en el objeto. `Balance += amount` es una forma corta de `Balance = Balance + amount`.

### Retiro

Escribe el test:

```csharp
[TestMethod]
public void Withdraw_SubtractsFromTheBalance()
{
    var account = new BankAccount(100m);

    account.Withdraw(30m);

    Assert.AreEqual(70m, account.Balance);
}
```

Agrega el método:

```csharp
public void Withdraw(decimal amount)
{
    Balance -= amount;
}
```

Verde. Ahora el objeto tiene dos operaciones que cambian el mismo estado, y la propiedad evita que quien llama lo cambie directamente.

### No permitir sobregiro

Una cuenta puede tener muchos saldos, pero tiene un invariante: el saldo nunca baja de cero. Escribe el test:

```csharp
[TestMethod]
public void Withdraw_MoreThanTheBalance_ThrowsAndKeepsTheBalance()
{
    var account = new BankAccount(100m);

    Assert.ThrowsException<InvalidOperationException>(() => account.Withdraw(150m));

    Assert.AreEqual(100m, account.Balance);
}
```

Ejecútalo. El test falla porque no se lanza ninguna excepción; `Withdraw` deja el saldo en `-50` sin problema. Agrega la comprobación antes de restar:

```csharp
public void Withdraw(decimal amount)
{
    if (amount > Balance)
    {
        throw new InvalidOperationException("Insufficient funds.");
    }

    Balance -= amount;
}
```

Verde. La segunda aserción importa tanto como la primera: la operación fallida tiene que dejar el objeto en su estado anterior. Comprueba la condición antes de mutar.

## Los objetos son referencias

Escribe un test más:

```csharp
[TestMethod]
public void TwoVariables_CanPointToTheSameAccount()
{
    var account = new BankAccount(100m);
    var alias = account;

    alias.Deposit(50m);

    Assert.AreEqual(150m, account.Balance);
}
```

Pasa sin cambiar el código de producción. `BankAccount` es un tipo por referencia: `account` guarda una referencia al objeto, y `alias = account` copia la referencia, no el objeto. Las dos variables apuntan al mismo saldo. Un `struct` o un `record struct` copiaría el valor; el siguiente capítulo mira esa diferencia.

Es la misma lección que los punteros en otros lenguajes, sin aritmética de punteros: dos nombres, un objeto, y una mutación visible desde ambos.

## Para cerrar

Lo que cubrimos:

- Una clase como plano y `new` como la forma de crear un objeto.
- Un constructor que le da al objeto su estado inicial.
- Una propiedad con getter público y setter privado.
- Métodos que mutan el estado y un tipo de retorno `void`.
- Un invariante comprobado antes de mutar, para que una operación fallida no cambie nada.
- Semántica de referencias: dos variables pueden apuntar al mismo objeto.

El capítulo sigue la progresión de tipos y métodos de [Structs, Methods and Interfaces en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces), con el comportamiento de referencias de [Pointers en Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors). .NET hace explícita la semántica de referencias con las clases y la de valores con los structs y los records.

Siguiente: [Inyección de dependencias](../dependency-injection/dependency-injection.es.md).

