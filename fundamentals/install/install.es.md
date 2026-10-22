# Instala .NET y prepara tu entorno

Las instrucciones oficiales de instalación de .NET están [aquí](https://dotnet.microsoft.com/download).

## Cómo funciona

### El SDK y el runtime

El SDK de .NET contiene los compiladores, el CLI y el runtime que necesitas para compilar y ejecutar código. El runtime solo puede ejecutar una aplicación que otra persona ya compiló; no puede compilar una. Instala el SDK.

Los proyectos de este libro apuntan a `net6.0` y CI los compila con el SDK `6.0.x`. Si instalas solo un SDK más nuevo, igual puedes compilar un proyecto `net6.0`, pero ejecutar sus tests necesita el runtime 6.0. Mantener el SDK de .NET 6 junto a los nuevos evita ese problema.

Mira qué hay en tu máquina:

```sh
dotnet --info
```

La salida lista los SDK y los runtimes instalados. Quieres al menos un SDK y un runtime `Microsoft.NETCore.App 6.0.x`:

```sh
dotnet --list-sdks
dotnet --list-runtimes
```

Si `dotnet` no se reconoce como comando, el instalador no lo puso en tu `PATH`. Cierra y vuelve a abrir la terminal primero; en macOS y Linux el instalador agrega la ruta a tu perfil de shell, y el cambio solo aplica a sesiones nuevas.

## Cómo probarlo

### Proyectos y soluciones

Un proyecto de .NET es un archivo `.csproj`: nombra el framework de destino, las referencias a paquetes y, para un programa de consola, el tipo de salida. Una solución es un archivo que agrupa proyectos para que un solo comando compile o pruebe todos.

Vas a usar cuatro comandos todo el tiempo:

```sh
dotnet new console -n HelloWorld -o src/HelloWorld
dotnet new classlib -n Calculator -o src/Calculator
dotnet new mstest -n Calculator.Tests -o tests/Calculator.Tests
dotnet new sln -n Calculator
```

Agregar las cosas a la solución y conectar la referencia es la parte que se olvida:

```sh
dotnet sln add src/Calculator/Calculator.csproj tests/Calculator.Tests/Calculator.Tests.csproj
dotnet add tests/Calculator.Tests/Calculator.Tests.csproj reference src/Calculator/Calculator.csproj
```

La referencia es lo que deja al proyecto de tests ver el código que prueba. Sin ella, el compilador dice que tus tipos no existen aunque los archivos estén en la misma carpeta.

Los comandos del día a día son cortos:

```sh
dotnet build
dotnet test
dotnet run --project src/HelloWorld
```

`dotnet test` restaura paquetes, compila la solución y ejecuta cada proyecto de tests que encuentra. En este libro, cada carpeta de capítulo es una solución por sí sola, así que puedes abrir `fundamentals/types` y ejecutar `dotnet test` sin compilar nada más.

### El editor

Puedes seguir todo el libro en VS Code con la extensión de C#, o en Visual Studio/Rider. El editor importa menos que lo que puedes hacer con él. Deberías poder hacer esto sin soltar el teclado:

- Renombrar un símbolo en toda la solución.
- Extraer una variable o un método de una selección.
- Ir a una definición y encontrar todos los usos.
- Formatear el archivo. `dotnet format` lo hace desde la terminal.
- Ejecutar un test suelto, o toda la suite, y ver el resultado.

## Escribe primero el test

La carpeta `fundamentals/install` contiene una solución pequeña con un test. Comprueba que la versión del runtime esté disponible sin fijar un número de parche:

```csharp
[TestClass]
public sealed class SdkInfoTests
{
    [TestMethod]
    public void GetRuntimeVersion_ReturnsNonEmpty()
    {
        var version = InstallCheck.SdkInfo.GetRuntimeVersion();

        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
    }
}
```

## Intenta ejecutar el test

Desde la carpeta del capítulo, ejecuta:

```sh
dotnet test
```

Al reconstruir este ejemplo pequeño antes de agregar `SdkInfo`, el compilador indica que `InstallCheck.SdkInfo` no existe.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega el tipo con un resultado provisional:

```csharp
namespace InstallCheck;

public static class SdkInfo
{
    public static string GetRuntimeVersion()
    {
        return string.Empty;
    }
}
```

El test compila, pero falla porque la cadena de versión está vacía.

## Escribe el código suficiente para que pase

Devuelve la versión del runtime:

```csharp
public static string GetRuntimeVersion()
{
    return System.Environment.Version.ToString();
}
```

Vuelve a ejecutar `dotnet test`. La solución incluye esta implementación y pasa un test. Eso demuestra que el SDK compiló C#, resolvió la referencia entre proyectos, el runner descubrió la clase de MSTest y el runtime ejecutó el método.

## Refactor

El ayudante solo devuelve `Environment.Version`; no hay otro comportamiento que extraer. Ejecuta el test otra vez y conserva la aserción amplia para que las actualizaciones de parche no la rompan.

## Repite con requisitos nuevos

### Diagnosticar un chequeo fallido

Si el comando falla, lee el error antes de cambiar algo:

- `dotnet: command not found` significa que falta el SDK o no está en `PATH`.
- `You must install .NET to run this application` significa que la compilación funcionó pero falta el runtime 6.0.
- Un `CS0246` sobre `InstallCheck` significa que falta la referencia al proyecto.

## Para cerrar

En este punto deberías tener:

- Un SDK de .NET 6 que `dotnet --info` puede ver.
- Una terminal donde `dotnet build` y `dotnet test` funcionan.
- Un editor que puede ejecutar los tests y hacer los refactors de los que depende este libro.
- El test de `fundamentals/install` en verde.

Siguiente: [Hola, mundo](../hello-world/hello-world.es.md), donde escribes tu primer programa y tu primer test.

