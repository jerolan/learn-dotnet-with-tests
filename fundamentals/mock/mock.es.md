# Mocking

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/mock)

Un mock es un doble de test que reemplaza una dependencia en el borde del código: un repositorio, un cliente HTTP, una cola de mensajes. El test controla lo que devuelve el doble y puede comprobar cómo lo usó el código bajo prueba. Moq construye esos dobles a partir de una interfaz.

Vamos a probar un servicio de usuarios que lee de un repositorio. La solución terminada está en `fundamentals/mock`. Si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores y agrega Moq al proyecto de tests:

```sh
dotnet add tests/Mocking.Tests/Mocking.Tests.csproj package Moq --version 4.20.70
```

## Cómo funciona

`Mock<IUserRepository>` crea un proxy de la interfaz. `Setup` define qué devuelve una llamada y `Verify` comprueba una interacción después de ejecutar el servicio. Por defecto, los mocks de Moq son permisivos (loose): una llamada sin configurar devuelve un valor por defecto. `MockBehavior.Strict` hace que las llamadas sin configurar lancen una excepción.

## Cómo probarlo

Entrega al servicio un repositorio mock que devuelva un usuario conocido. Comprueba el nombre devuelto y verifica que el repositorio recibió el id pedido una vez. Verifica solo las interacciones que forman parte del contrato.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Mocking.Tests;

[TestClass]
public sealed class UserServiceTests
{
    [TestMethod]
    public void GetName_ReturnsTheNameFromTheRepository()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.Find(1)).Returns(new User("Ada"));
        var service = new UserService(repository.Object);

        var name = service.GetName(1);

        Assert.AreEqual("Ada", name);
        repository.Verify(r => r.Find(1), Times.Once);
    }
}
```

Hay dos aserciones en este test. `Assert.AreEqual` comprueba el resultado; `Verify` comprueba que el servicio pidió el id correcto exactamente una vez. La segunda es la prueba de interacción.

## Intenta ejecutar el test

El compilador todavía no encuentra los tipos de producción:

```text
error CS0246: The type or namespace name 'IUserRepository' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Crea el contrato, el usuario y el servicio con un resultado provisional:

```csharp
namespace Mocking;

public sealed record User(string Name);

public interface IUserRepository
{
    User? Find(int id);
}

public sealed class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public string GetName(int id)
    {
        return string.Empty;
    }
}
```

El test compila y falla con `Expected:<Ada>. Actual:<>`.

## Escribe el código suficiente para que pase

```csharp
public string GetName(int id)
{
    var user = _repository.Find(id);

    return user?.Name ?? "Unknown";
}
```

Verde, incluido el `Verify`. El servicio depende de la interfaz; el test entrega un mock que responde a `Find(1)` con un usuario.

## Refactor

El servicio hace una llamada al repositorio y devuelve el resultado directamente. Ejecuta el test otra vez; no hay duplicación de producción que quitar.

## Repite con requisitos nuevos

### Un usuario desconocido

Escribe el test:

```csharp
[TestMethod]
public void GetName_UnknownUser_ReturnsUnknown()
{
    var repository = new Mock<IUserRepository>();
    repository.Setup(r => r.Find(99)).Returns((User?)null);
    var service = new UserService(repository.Object);

    var name = service.GetName(99);

    Assert.AreEqual("Unknown", name);
}
```

Pasa con la implementación actual, porque `user?.Name ?? "Unknown"` maneja el null. El test fija el valor de reemplazo: si alguien cambia el método a `return user.Name`, el test falla con `NullReferenceException` en vez de publicar un crash.

El cast `(User?)null` le dice a Moq qué sobrecarga de `Returns` quieres. Sin él, el compilador no puede inferir el tipo nullable del valor de retorno.

### Elegir qué mockear

No hay código de producción que cambiar. La decisión de diseño es dónde va el mock.

Mockea el borde que tú controlas: `IUserRepository` es nuestra interfaz, y una implementación real iría a una base de datos. El mock reemplaza la base de datos, no la lógica de negocio. Si un test mockea un colaborador interno que no tiene E/S, un fake escrito a mano suele ser más corto y más claro; el capítulo de inyección de dependencias usó uno.

No mockees tipos que no controlas, como `HttpClient` o un SDK de terceros. Envuélvelos en tu propia interfaz y mockea el envoltorio. De lo contrario, el test queda acoplado a una API que no puedes cambiar.

Verifica las interacciones que importan. "Pedimos el usuario 1 una vez" es un contrato. "Llamamos a `Find` antes de leer `Name`" suele ser un detalle de implementación; afirmarlo hace que cada refactor rompa el test.

## Para cerrar

Lo que cubrimos:

- Crear un mock a partir de una interfaz con `new Mock<T>()`.
- Configurar un valor de retorno con `Setup` y `Returns`.
- Pasar `mock.Object` como dependencia del constructor.
- Verificar una interacción con `Verify` y `Times.Once`.
- Comportamiento permisivo frente a estricto.
- Elegir un mock en un borde de E/S y un fake para un colaborador interno.

El capítulo sigue el [capítulo Mocking de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/mocking): el test reemplaza un colaborador lento o externo con un doble controlado. .NET agrega el proxy de interfaz y la API de verificación de Moq.

Siguiente: [Structs y records](../records/records.es.md).

