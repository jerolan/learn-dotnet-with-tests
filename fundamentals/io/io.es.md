# Archivos e I/O

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/io)

La E/S es leer y escribir algo fuera del programa: un archivo, un flujo de red, la entrada estándar. .NET la modela con lectores y escritores, así el mismo código de parseo funciona contra un archivo en producción y contra una cadena en memoria en un test.

Vamos a construir un lector y escritor de CSV simple sobre `TextReader` y `TextWriter`. La solución terminada está en `fundamentals/io`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Io`, un proyecto de MSTest en `tests/Io.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Io.Tests;

[TestClass]
public sealed class CsvTests
{
    [TestMethod]
    public void Read_ParsesRows()
    {
        var rows = Csv.Read(new StringReader("a,b\nc,d"));

        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
    }
}
```

`StringReader` es un `TextReader` sobre una cadena. El test no toca el disco.

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'Csv' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

```csharp
namespace Io;

public static class Csv
{
    public static IReadOnlyList<string[]> Read(TextReader reader)
    {
        return Array.Empty<string[]>();
    }
}
```

El test compila y falla con `Expected:<2>. Actual:<0>`.

## Escribe el código suficiente para que pase

```csharp
public static IReadOnlyList<string[]> Read(TextReader reader)
{
    var rows = new List<string[]>();
    string? line;

    while ((line = reader.ReadLine()) != null)
    {
        rows.Add(line.Split(','));
    }

    return rows;
}
```

Verde. `ReadLine()` devuelve la siguiente línea o `null` al final de la entrada. La condición del `while` asigna la línea y comprueba el final a la vez. Cada línea se divide por comas en una fila.

El método recibe un `TextReader`, no una ruta de archivo. Esa es la costura: un archivo real, una cadena en memoria y un flujo de red proveen un `TextReader`.

## Repite con requisitos nuevos

### Líneas en blanco

Escribe el test:

```csharp
[TestMethod]
public void Read_SkipsBlankLines()
{
    var rows = Csv.Read(new StringReader("a,b\n\n  \nc,d"));

    Assert.AreEqual(2, rows.Count);
}
```

Falla porque las líneas vacías y las de solo espacios se convierten en filas vacías. Sáltalas:

```csharp
if (string.IsNullOrWhiteSpace(line))
{
    continue;
}

rows.Add(line.Split(','));
```

Verde.

### Escritura

Escribe el test:

```csharp
[TestMethod]
public void Write_WritesRows()
{
    var writer = new StringWriter();

    Csv.Write(writer, new[]
    {
        new[] { "a", "b" },
        new[] { "c", "d" }
    });

    var expected = "a,b" + Environment.NewLine + "c,d" + Environment.NewLine;

    Assert.AreEqual(expected, writer.ToString());
}
```

Agrega el método:

```csharp
public static void Write(TextWriter writer, IEnumerable<string[]> rows)
{
    foreach (var row in rows)
    {
        writer.WriteLine(string.Join(",", row));
    }
}
```

Verde. `StringWriter` captura la salida en memoria. `WriteLine` usa la secuencia de salto de línea del escritor, así el test compara contra `Environment.NewLine` en vez de fijar `\n`.

### Ida y vuelta

Escribe el test:

```csharp
[TestMethod]
public void RoundTrip_PreservesRows()
{
    var writer = new StringWriter();

    Csv.Write(writer, new[] { new[] { "a", "b" } });

    var rows = Csv.Read(new StringReader(writer.ToString()));

    CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
}
```

Pasa. El escritor y el lector coinciden en el formato, y el test verifica las dos mitades juntas.

## Refactor

El parser es simple a propósito. Divide por comas y no maneja campos entre comillas, comas embebidas ni saltos de línea dentro de un campo. Un CSV real necesita un parser que entienda comillas; el punto aquí es la costura del lector/escritor, no la corrección de CSV. Dilo en el código o usa una biblioteca cuando el formato importe.

Para un archivo real, abre un lector o escritor y libéralo:

```csharp
using var reader = File.OpenText("orders.csv");
var rows = Csv.Read(reader);
```

`File.OpenText` devuelve un `StreamReader`, que es un `TextReader`. `File.CreateText` devuelve un `StreamWriter`. La sentencia `using` cierra el archivo aunque el parseo lance una excepción; el siguiente capítulo lo cubre.

Leer con `ReadLine()` mantiene solo una línea en memoria. `File.ReadAllLines` carga el archivo entero, lo cual está bien para un archivo pequeño y mal para uno grande. La misma elección aparece con E/S asíncrona: `ReadLineAsync()` y `WriteLineAsync()` dejan esperar sin bloquear un hilo.

## Para cerrar

Lo que cubrimos:

- `TextReader` y `TextWriter` como costura sobre archivos y memoria.
- `StringReader` y `StringWriter` para tests sin disco.
- `ReadLine()` devolviendo `null` al final de la entrada.
- Dividir y unir filas de CSV simple.
- Saltar líneas en blanco.
- Un test de ida y vuelta entre lector y escritor.
- Streaming frente a leer el archivo entero, y el `using` que necesitan los manejadores de archivo.

El capítulo sigue el [capítulo I/O de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/io): el código de dominio lee de una fuente abstracta y un test entrega una en memoria. .NET divide la abstracción en `TextReader` y `TextWriter`.

Siguiente: [La palabra using](../using/using.es.md).

