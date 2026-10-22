# Inyección de dependencias

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/dependency-injection)

Una clase que crea sus propias dependencias es difícil de probar y de cambiar. La inyección de dependencias significa que la clase recibe lo que necesita desde fuera, normalmente por el constructor. La clase conserva su comportamiento; quien llama decide qué implementación entregar.

Vamos a construir un notificador que manda una alerta por uno o más canales. La solución terminada está en `fundamentals/dependency-injection`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/DependencyInjection`, un proyecto de MSTest en `tests/DependencyInjection.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DependencyInjection.Tests;

[TestClass]
public sealed class NotifierTests
{
    [TestMethod]
    public void Notify_SendsTheFormattedMessage()
    {
        var sender = new RecordingSender();
        var notifier = new Notifier(sender);

        notifier.Notify("Disk full");

        Assert.AreEqual(1, sender.Messages.Count);
        Assert.AreEqual("[Alert] Disk full", sender.Messages[0]);
    }

    private sealed class RecordingSender : IMessageSender
    {
        public List<string> Messages { get; } = new List<string>();

        public void Send(string message)
        {
            Messages.Add(message);
        }
    }
}
```

`RecordingSender` es un fake: implementa el contrato y registra lo que recibe. El test no necesita un servidor de correo ni un teléfono; necesita un emisor que pueda inspeccionar.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'IMessageSender' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Crea el contrato y un notificador que lo acepte:

```csharp
namespace DependencyInjection;

public interface IMessageSender
{
    void Send(string message);
}

public sealed class Notifier
{
    private readonly IMessageSender _sender;

    public Notifier(IMessageSender sender)
    {
        _sender = sender;
    }

    public void Notify(string message)
    {
    }
}
```

El test compila y falla con `Expected:<1>. Actual:<0>` en el conteo de mensajes.

## Escribe el código suficiente para que pase

```csharp
public void Notify(string message)
{
    _sender.Send($"[Alert] {message}");
}
```

Verde. `Notifier` no sabe si el emisor escribe correo, SMS o un log. Conoce la interfaz y la llama. El constructor es el punto de inyección: el test pasa un fake, y producción pasa un emisor real.

## Repite con requisitos nuevos

### Más de un canal

Escribe el test:

```csharp
[TestMethod]
public void Notify_SendsToEverySender()
{
    var email = new RecordingSender();
    var sms = new RecordingSender();
    var notifier = new Notifier(new IMessageSender[] { email, sms });

    notifier.Notify("Disk full");

    Assert.AreEqual("[Alert] Disk full", email.Messages[0]);
    Assert.AreEqual("[Alert] Disk full", sms.Messages[0]);
}
```

El compilador dice que no hay un constructor que reciba un arreglo. Cambia la dependencia a una colección:

```csharp
public sealed class Notifier
{
    private readonly IEnumerable<IMessageSender> _senders;

    public Notifier(IEnumerable<IMessageSender> senders)
    {
        _senders = senders;
    }

    public void Notify(string message)
    {
        var alert = $"[Alert] {message}";

        foreach (var sender in _senders)
        {
            sender.Send(alert);
        }
    }
}
```

El primer test deja de compilar porque `Notifier` ahora espera una colección. Actualízalo para pasar el fake único en un arreglo:

```csharp
var notifier = new Notifier(new IMessageSender[] { sender });
```

Verde. El notificador sigue sin saber cuántos canales existen ni qué hacen. Quien llama lo decide; un test puede pasar dos fakes, y la aplicación puede pasar correo y SMS.

## Refactor

El diseño ya es pequeño, pero la decisión importante es dónde viven los tipos concretos. `Notifier` nunca escribe `new EmailSender()`. En algún lugar del arranque de la aplicación, una raíz de composición (composition root) hace:

```csharp
IMessageSender[] senders =
{
    new EmailSender(),
    new SmsSender()
};

var notifier = new Notifier(senders);
```

La raíz de composición puede conocer las clases concretas. Todo lo demás depende de `IMessageSender`. Eso mantiene el notificador testeable y permite agregar un canal sin editarlo.

La inyección por constructor es el valor por defecto porque hace la dependencia obligatoria y visible en la firma. Una clase que busca un service locator estático esconde la misma dependencia y vuelve menos obvia la preparación del test.

El contenedor de una aplicación grande es una comodidad para construir este grafo. El concepto es el mismo: la clase declara lo que necesita, y algo fuera lo crea. El contenedor de ASP.NET Core aparece en la Parte 3; el notificador no lo necesita para ser correcto.

## Para cerrar

Lo que cubrimos:

- Una dependencia como parámetro del constructor, tipada como interfaz.
- Un fake que registra llamadas para que el test las inspeccione.
- Inyección por constructor de un emisor y después de una colección de emisores.
- La raíz de composición como el único lugar que conoce los tipos concretos.
- La inyección por constructor como valor por defecto frente a un service locator escondido.

El capítulo sigue el capítulo de dependency injection de [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/dependency-injection): la lógica de dominio recibe su efecto secundario a través de una interfaz, y el test entrega un sustituto. .NET agrega la inyección por constructor y, más adelante, un contenedor.

Siguiente: [Mocking](../mock.md).

