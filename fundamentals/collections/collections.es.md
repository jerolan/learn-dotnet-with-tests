# Colecciones

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/collections)

Un arreglo tiene longitud fija. Una `List<T>` crece. Un `Dictionary<TKey, TValue>` busca valores por clave, y un `HashSet<T>` responde rápido si un valor está presente. Elegir el correcto cambia cómo se lee el código y qué tan rápido corre.

Vamos a construir un contador de palabras: leer un texto, contar cada palabra y devolver las más comunes. La solución terminada está en `fundamentals/collections`; si la estás armando desde cero, usa la misma estructura que en los capítulos anteriores: una biblioteca de clases en `src/Collections`, un proyecto de MSTest en `tests/Collections.Tests`, una solución y una referencia entre proyectos.

## Escribe primero el test

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Collections.Tests;

[TestClass]
public sealed class WordCounterTests
{
    [TestMethod]
    public void Counts_CountsEachWord()
    {
        var counter = new WordCounter("the cat and the dog");

        Assert.AreEqual(2, counter.Counts["the"]);
        Assert.AreEqual(1, counter.Counts["cat"]);
        Assert.AreEqual(1, counter.Counts["and"]);
        Assert.AreEqual(1, counter.Counts["dog"]);
    }
}
```

## Intenta ejecutar el test

```text
error CS0246: The type or namespace name 'WordCounter' could not be found
```

## Escribe el código mínimo para que el test compile y mira la salida

Empieza por la forma del resultado:

```csharp
namespace Collections;

public sealed class WordCounter
{
    public WordCounter(string text)
    {
    }

    public IReadOnlyDictionary<string, int> Counts { get; } =
        new Dictionary<string, int>();
}
```

El código compila y el test falla con una `KeyNotFoundException` para `the`. El diccionario está vacío.

## Escribe el código suficiente para que pase

```csharp
public sealed class WordCounter
{
    private readonly Dictionary<string, int> _counts;

    public WordCounter(string text)
    {
        _counts = CountWords(text);
    }

    public IReadOnlyDictionary<string, int> Counts => _counts;

    private static Dictionary<string, int> CountWords(string text)
    {
        var counts = new Dictionary<string, int>();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            counts.TryGetValue(word, out var current);
            counts[word] = current + 1;
        }

        return counts;
    }
}
```

Verde. `Dictionary<string, int>` asocia una palabra con su conteo. `TryGetValue` busca el conteo actual y devuelve `false` cuando la clave es nueva; `out var current` te da `0` en ese caso. La asignación por índice crea o actualiza la entrada.

`IReadOnlyDictionary` expone el mapa sin dejar que quien llama agregue o quite entradas. El campo sigue privado y mutable; la propiedad es la vista pública.

## Repite con requisitos nuevos

### Más que espacios

Escribe el test:

```csharp
[TestMethod]
public void Counts_IgnoresExtraWhitespace()
{
    var counter = new WordCounter("  the\tcat\nand  the  ");

    Assert.AreEqual(2, counter.Counts["the"]);
    Assert.AreEqual(1, counter.Counts["cat"]);
}
```

Falla porque el split solo conoce el espacio: `cat` llega como `"cat\nand"` y el conteo de `cat` queda mal. Dale al split un conjunto explícito de separadores:

```csharp
var separators = new[] { ' ', '\t', '\n', '\r' };

foreach (var word in text.Split(separators, StringSplitOptions.RemoveEmptyEntries))
```

Verde. `RemoveEmptyEntries` descarta las cadenas vacías que producen los separadores repetidos; sin él, los espacios del principio y los dobles crearían claves vacías.

### Las mayúsculas no crean otra palabra

Escribe el test:

```csharp
[TestMethod]
public void Counts_IsCaseInsensitive()
{
    var counter = new WordCounter("The the THE");

    Assert.AreEqual(3, counter.Counts["the"]);
}
```

Falla con `Expected:<3>. Actual:<2>` porque `The` y `THE` son claves distintas. Normaliza antes de buscar:

```csharp
var key = word.ToLowerInvariant();

counts.TryGetValue(key, out var current);
counts[key] = current + 1;
```

Verde. `ToLowerInvariant` evita las reglas de mayúsculas de la cultura actual, así que el conteo no cambia con el idioma de la máquina.

### Las palabras más comunes

Un diccionario no está ordenado por valor, así que el ranking necesita una lista aparte. Escribe el test:

```csharp
[TestMethod]
public void MostCommon_ReturnsTheMostFrequentWords()
{
    var counter = new WordCounter("the cat and the dog");

    var top = counter.MostCommon(2);

    Assert.AreEqual(2, top.Count);
    Assert.AreEqual("the", top[0]);
    Assert.AreEqual("and", top[1]);
}
```

El compilador dice que falta `MostCommon`. Agrégala:

```csharp
public IReadOnlyList<string> MostCommon(int count)
{
    var entries = new List<KeyValuePair<string, int>>(_counts);

    entries.Sort((left, right) =>
    {
        var byCount = right.Value.CompareTo(left.Value);

        return byCount != 0
            ? byCount
            : string.CompareOrdinal(left.Key, right.Key);
    });

    var result = new List<string>();

    for (var i = 0; i < count && i < entries.Count; i++)
    {
        result.Add(entries[i].Key);
    }

    return result;
}
```

Verde. `new List<KeyValuePair<string, int>>(_counts)` copia el diccionario a una lista que se puede ordenar. La comparación pone primero el conteo más alto; cuando los conteos son iguales, compara las claves para que el resultado sea determinista. El bucle se detiene en `count` o al final de la lista, lo que llegue primero.

Agrega el borde:

```csharp
[TestMethod]
public void MostCommon_ReturnsEverythingWhenAskedForMore()
{
    var counter = new WordCounter("a b");

    var top = counter.MostCommon(10);

    Assert.AreEqual(2, top.Count);
    Assert.AreEqual("a", top[0]);
    Assert.AreEqual("b", top[1]);
}
```

Pasa por la condición `i < entries.Count`.

## Refactor

El código es pequeño y cada colección tiene un trabajo:

- `Dictionary<string, int>` guarda los conteos y da búsqueda por palabra en tiempo constante.
- `List<KeyValuePair<string, int>>` es la copia ordenable. Ordenar una lista está permitido; ordenar un diccionario directamente no, porque un diccionario no tiene orden.
- `IReadOnlyDictionary` y `IReadOnlyList` son los contratos públicos. Dicen que quien llama puede leer pero no mutar el almacenamiento interno.

Un arreglo encajaría mal para los conteos: las claves son palabras, no índices, y la cantidad de palabras distintas se desconoce hasta leer el texto. Una `List<string>` necesitaría una búsqueda lineal por cada palabra, lo que vuelve el conteo cuadrático. Un `HashSet<string>` es la colección correcta cuando solo necesitas pertenencia, no un conteo; responde `Contains` rápido y rechaza duplicados.

## Para cerrar

Lo que cubrimos:

- `Dictionary<TKey, TValue>` para conteos y `TryGetValue` con una variable `out`.
- `string.Split` con separadores explícitos y `RemoveEmptyEntries`.
- Normalización de mayúsculas con `ToLowerInvariant`.
- `List<T>` como copia ordenable de un diccionario.
- `Sort` con una comparación que rompe empates de forma determinista.
- `IReadOnlyDictionary` y `IReadOnlyList` como vistas públicas.
- Cuándo conviene un arreglo, una lista, un diccionario o un conjunto.

El capítulo sigue el [capítulo Maps de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/maps): un mapa guarda conteos, una clave ausente es parte del comportamiento y los tests fijan la búsqueda. .NET agrega las interfaces de solo lectura y el paso por lista ordenada para un ranking.

Siguiente: [Genéricos](../generics/generics.es.md).

