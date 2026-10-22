# Hola, mundo

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/hello-world)

Es tradición que tu primer programa en un lenguaje nuevo sea [Hello, World](https://es.wikipedia.org/wiki/%C2%A1Hola,_mundo!).

Crea una carpeta para tu trabajo y pídele al CLI de .NET un proyecto de consola:

```sh
mkdir hello-world
cd hello-world
dotnet new console -n HelloWorld -o src/HelloWorld
```

Abre `src/HelloWorld/Program.cs` y reemplaza lo que escribió la plantilla por:

```csharp
Console.WriteLine("Hello, world");
```

Ejecútalo:

```sh
dotnet run --project src/HelloWorld
```

Deberías ver:

```text
Hello, world
```

## Cómo funciona

`Program.cs` es un programa de nivel superior. Escribes sentencias al principio del archivo y el compilador las envuelve en un método `Main` generado. La ejecución empieza en la primera sentencia y baja por el archivo.

`Console.WriteLine` escribe una línea en la salida estándar. `Console` vive en el espacio de nombres `System`, y la plantilla de consola activa los usings implícitos, así que no necesitas `using System;` al principio.

Cambia el texto entre comillas, vuelve a ejecutar el comando y mira cómo cambia la salida. Por ahora el programa es una lista de sentencias.

## Cómo probarlo

¿Cómo pruebas esto? `Console.WriteLine` es un efecto secundario: sale del programa para imprimir en tu terminal. El saludo es nuestro dominio, y podemos probarlo sin una consola.

Mueve el saludo a una clase `Greeter` y deja que `Program.cs` se encargue de imprimir el resultado:

```csharp
using HelloWorld;

var greeter = new Greeter();
Console.WriteLine(greeter.SayHello());
```

Cuando exista el método de `Greeter`, esto conservará la misma salida para quien usa el programa y dejará el saludo como una llamada normal a un método.

### Proyectos, referencias y una solución

.NET no va a descubrir un archivo de tests que esté junto a tu código. Los tests viven en su propio proyecto, y ese proyecto tiene que referenciar al proyecto que prueba. Esta es la parte donde las herramientas piden un poco de ceremonia antes de que puedas ver un test en rojo.

Desde la carpeta del capítulo, crea el proyecto de tests, una solución que agrupe ambos proyectos, y la referencia:

```sh
dotnet new mstest -n HelloWorld.Tests -o tests/HelloWorld.Tests
dotnet new sln -n HelloWorld
dotnet sln add src/HelloWorld/HelloWorld.csproj tests/HelloWorld.Tests/HelloWorld.Tests.csproj
dotnet add tests/HelloWorld.Tests/HelloWorld.Tests.csproj reference src/HelloWorld/HelloWorld.csproj
```

`dotnet new mstest` escribe un proyecto que ya referencia el SDK de tests, MSTest y su adaptador. La referencia entre proyectos importa cuando exista `Greeter`: sin ella, el proyecto de tests no puede ver la clase aunque el archivo esté ahí mismo.

Borra el `UnitTest1.cs` generado. Solo existe para demostrar que la plantilla corrió.

El repositorio incluye la solución terminada de este capítulo. Si creaste tu propio proyecto, usa los mismos nombres; si no, abre `fundamentals/hello-world` y ejecuta los comandos desde ahí.

## Escribe primero el test

Crea `tests/HelloWorld.Tests/GreeterTests.cs`:

Este test describe el saludo después de separarlo de la consola. La implementación saldrá del fallo del test.

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HelloWorld.Tests;

[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void TestHello()
    {
        var greeter = new Greeter();

        var got = greeter.SayHello();
        var want = "Hello, world";

        Assert.AreEqual(want, got);
    }
}
```

### Convenciones de los tests

La clase lleva `[TestClass]` y cada método de test lleva `[TestMethod]`. Las aserciones vienen de `Assert`; `Assert.AreEqual(expected, actual)` es la que más vas a usar. El nombre del método es la descripción. MSTest lo imprime junto con el mensaje de aserción cuando un test falla.

## Intenta ejecutar el test

Ejecuta los tests desde la carpeta del capítulo:

```sh
dotnet test
```

El test todavía no compila porque `Greeter` no existe:

```text
error CS0246: The type or namespace name 'Greeter' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Crea la clase y devuelve una cadena vacía por ahora:

```csharp
namespace HelloWorld;

public class Greeter
{
    public string SayHello()
    {
        return string.Empty;
    }
}
```

El test compila y falla con `Expected:<Hello, world>. Actual:<>`.

## Escribe el código suficiente para que pase

Devuelve el saludo:

```csharp
public string SayHello()
{
    return "Hello, world";
}
```

Ejecuta los tests. Verde. Cambia `want` por `"Hello, world!"` y vuelve a ejecutar para ver una aserción roja con los dos valores; después restaura la cadena esperada.

## Refactor

El método de saludo devuelve directamente una cadena y no tiene comportamiento duplicado que extraer. Ejecuta los tests otra vez; siguen en verde. Conserva esta implementación hasta que un requisito nuevo le dé más trabajo al método.

## Repite con requisitos nuevos

### Saludar a una persona

Ahora escribiremos primero el test de cada requisito nuevo y haremos crecer el método.

El siguiente requisito es dejar que quien llama elija a quién saludamos.

Empieza por el test:

```csharp
[TestMethod]
public void SayHelloToChris()
{
    var greeter = new Greeter();

    var got = greeter.SayHello("Chris");
    var want = "Hello, Chris";

    Assert.AreEqual(want, got);
}
```

Ejecuta `dotnet test` y obtienes un error de compilación:

```text
error CS1501: No overload for method 'SayHello' takes 1 arguments
```

Escucha al compilador. Te está diciendo la forma que necesita el código: `SayHello` tiene que aceptar un argumento.

Cambia el método para que reciba un `string`:

```csharp
public string SayHello(string name)
{
    return "Hello, world";
}
```

El proyecto de tests ya compila, pero `Program.cs` no: sigue llamando a `SayHello()` sin argumento. Pasa `"world"` en `Program.cs`:

```csharp
Console.WriteLine(greeter.SayHello("world"));
```

Vuelve a ejecutar los tests. El código compila y el test falla por la razón correcta:

```text
  Failed SayHelloToChris [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<Hello, Chris>. Actual:<Hello, world>.
```

Hazlo pasar usando el argumento:

```csharp
public string SayHello(string name)
{
    return "Hello, " + name;
}
```

Ejecuta los tests. Verde.

### Una nota sobre el control de versiones

Si usas control de versiones, haz commit del código tal como está. Tienes software funcionando respaldado por un test. No hagas push a main todavía si vas a refactorizar en la sección siguiente; el commit es un punto al que puedes volver si el refactor sale mal.

### Extraer el prefijo del saludo

No hay mucho que refactorizar, pero podemos darle un nombre al prefijo del saludo:

```csharp
private const string EnglishHelloPrefix = "Hello, ";

public string SayHello(string name)
{
    return EnglishHelloPrefix + name;
}
```

Ejecuta los tests después del cambio. Una constante captura el significado del valor y evita repetir la misma cadena en varios lugares.

### Saludar al mundo si el nombre está vacío

El siguiente requisito: cuando quien llama pasa una cadena vacía, saluda al mundo en vez de devolver `Hello, `.

Escribe primero el test nuevo:

```csharp
[TestMethod]
public void SayHelloToWorldWhenEmptyStringIsSupplied()
{
    var greeter = new Greeter();

    var got = greeter.SayHello(string.Empty);
    var want = "Hello, World";

    Assert.AreEqual(want, got);
}
```

Ejecuta los tests. El nuevo falla con `Expected:<Hello, World>. Actual:<Hello, >`.

Arregla el código con un `if`:

```csharp
public string SayHello(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        name = "World";
    }

    return EnglishHelloPrefix + name;
}
```

Vuelve a ejecutar los tests. Pasan los dos, y el comportamiento anterior sigue funcionando.

Los tests comparan mensajes de la misma forma, y esa repetición está en los tests. El refactor no es solo para el código de producción:

```csharp
[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void SayHelloToPeople()
    {
        AssertCorrectMessage("Hello, Chris", new Greeter().SayHello("Chris"));
    }

    [TestMethod]
    public void SayHelloToWorldWhenEmptyStringIsSupplied()
    {
        AssertCorrectMessage("Hello, World", new Greeter().SayHello(string.Empty));
    }

    private static void AssertCorrectMessage(string want, string got)
    {
        Assert.AreEqual(want, got);
    }
}
```

La aserción vive ahora en un ayudante, y los tests se leen como una lista de ejemplos. `TestHello` y `SayHelloToChris` desaparecieron porque el método ya no tiene una forma sin parámetros; los dos tests que quedan describen el requisito tal como está.

Ejecuta los tests una vez más. Siguen en verde.

## Para cerrar

Lo que cubrimos:

- Crear un proyecto de consola y ejecutarlo con `dotnet run`.
- Separar el saludo, nuestro dominio, de `Console.WriteLine`, un efecto secundario.
- Crear un proyecto de tests, una solución y una referencia entre proyectos.
- Los atributos de MSTest y `Assert.AreEqual`.
- Rojo, verde, refactor con dos requisitos nuevos: un argumento con el nombre y una cadena vacía.
- Una constante y un refactor en los tests.

La progresión sigue el [capítulo Hello, World de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/hello-world): empieza con un programa que imprime, saca la lógica del punto de entrada, escribe un test, haz crecer la función con un nombre y después maneja la cadena vacía.

Siguiente: [Tipos y variables](../types/types.es.md).

