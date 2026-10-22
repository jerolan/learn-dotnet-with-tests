# La palabra using

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/using)

Algunos objetos son dueños de un recurso que debe liberarse: un manejador de archivo, un socket, una conexión a base de datos. La sentencia `using` llama a `Dispose()` cuando termina el ámbito, tanto si el código terminó normalmente como si lanzó una excepción. Esa garantía es la razón por la que casi nunca llamas `Dispose()` a mano.

Vamos a construir una sesión pequeña que registra cuándo se abre y se cierra. La solución terminada está en `fundamentals/using`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/UsingKeyword`, un proyecto de MSTest en `tests/UsingKeyword.Tests`, una solución y una referencia entre proyectos.

## Cómo funciona

`IDisposable` expone `Dispose()` para liberar los recursos de un objeto. Un ámbito `using` se convierte en un `try`/`finally`, así la limpieza ocurre al salir normalmente o por una excepción. `using var` dura hasta el final del bloque que lo contiene; la forma con paréntesis permite elegir un ámbito menor. La recolección de basura administra memoria, mientras `Dispose()` libera recursos como manejadores de archivo y sockets.

```csharp
using (var session = new Session(log))
{
    // la sesión se libera aquí
}

using var other = new Session(log);
// other se libera al final del bloque que lo contiene
```

## Cómo probarlo

Entrega al objeto descartable un log que registre la apertura y la liberación. Comprueba que se liberó y que la limpieza ocurrió en el momento esperado. Una excepción y la liberación repetida son casos distintos del ciclo de vida.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UsingKeyword.Tests;

[TestClass]
public sealed class SessionTests
{
    [TestMethod]
    public void Run_DisposesTheSession()
    {
        var log = new List<string>();
        Session? captured = null;

        SessionRunner.Run(log, session => captured = session);

        Assert.IsTrue(captured!.IsDisposed);
        CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Session' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace UsingKeyword;

public sealed class Session : IDisposable
{
    private readonly List<string> _log;

    public Session(List<string> log)
    {
        _log = log;
        _log.Add("open");
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
        _log.Add("dispose");
    }
}

public static class SessionRunner
{
    public static void Run(List<string> log, Action<Session> action)
    {
        var session = new Session(log);
        action(session);
    }
}
```

El test compila y falla: la sesión nunca se libera, así que `IsDisposed` es false y el log solo tiene `open`.

## Escribe el código suficiente para que pase

```csharp
public static void Run(List<string> log, Action<Session> action)
{
    using var session = new Session(log);
    action(session);
}
```

Verde. Al final de `Run`, el compilador inserta un bloque `finally` que llama a `session.Dispose()`. El cuerpo puede lanzar, retornar o terminar normalmente; el bloque finally igual corre.

## Refactor

La declaración `using var` hace explícita la vida de la sesión, y el test cubre el comportamiento visible para quien llama. Ejecuta el test otra vez; no hace falta otro ayudante de limpieza.

## Repite con requisitos nuevos

### Liberación con una excepción

Escribe el test:

```csharp
[TestMethod]
public void Run_DisposesWhenTheActionThrows()
{
    var log = new List<string>();

    Assert.ThrowsException<InvalidOperationException>(() =>
        SessionRunner.Run(log, _ => throw new InvalidOperationException()));

    CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
}
```

Pasa. La excepción sale de `Run`, el bloque finally corre y quien llama sigue viendo la excepción original. `using` limpia; no se traga errores.

### La forma de bloque

Escribe el test:

```csharp
[TestMethod]
public void UsingStatement_DisposesAtTheEndOfTheBlock()
{
    var log = new List<string>();
    Session? captured;

    using (var session = new Session(log))
    {
        captured = session;
        Assert.IsFalse(session.IsDisposed);
    }

    Assert.IsTrue(captured!.IsDisposed);
}
```

Pasa. La sesión está viva dentro del bloque y se libera inmediatamente después de la llave de cierre.

### Dispose dos veces

Escribe el test:

```csharp
[TestMethod]
public void Dispose_IsIdempotent()
{
    var log = new List<string>();
    var session = new Session(log);

    session.Dispose();
    session.Dispose();

    CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
}
```

Falla porque `Dispose` registra dos veces. Hazlo idempotente:

```csharp
public void Dispose()
{
    if (IsDisposed)
    {
        return;
    }

    IsDisposed = true;
    _log.Add("dispose");
}
```

Verde. Una segunda llamada a `Dispose()` es inofensiva. La convención es que liberar pueda ocurrir más de una vez, sobre todo cuando un bloque `using` y un finalizador u otro dueño liberan el mismo recurso.

### Mantener clara la propiedad del recurso

Algunas reglas mantienen clara la propiedad:

- No llames `Dispose()` sobre un objeto que ya es dueño de una sentencia `using`. Elige un solo dueño.
- No liberes un objeto que devuelves a quien llama. La propiedad se transfiere con el valor de retorno; el llamador decide cuándo liberarlo.
- Prefiere `using` a un `try`/`finally` manual. La versión del compilador es más difícil de equivocar.
- Para limpieza asíncrona, implementa `IAsyncDisposable` y usa `await using`. Los streams y las conexiones modernas de .NET suelen soportarlo.
- Mantén el ámbito de `using` tan pequeño como necesite el recurso. Una declaración al principio de un método largo mantiene el archivo abierto más de lo necesario.

`using` también se usa para importar un espacio de nombres (`using System.Text;`). Los dos significados son distintos: la sentencia libera, la directiva importa. El compilador los distingue por la posición.

## Para cerrar

Lo que cubrimos:

- `IDisposable` y `Dispose()`.
- `using` como un `try`/`finally` que siempre libera.
- Declaraciones `using var` y la sentencia con paréntesis.
- Liberación en el camino de excepción.
- Liberación idempotente.
- Memoria administrada frente a recursos no administrados, y `IAsyncDisposable` para limpieza asíncrona.

El capítulo sigue la disciplina de recursos del [capítulo I/O de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/io): quien abre un recurso es responsable de cerrarlo a tiempo. .NET hace explícita esa propiedad con `IDisposable` y `using`.

Con esto termina la Parte 1. La Parte 2 cubre los fundamentos de testing.

