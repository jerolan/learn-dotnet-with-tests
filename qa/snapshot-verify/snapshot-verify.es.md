# Snapshot testing con Verify

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/snapshot-verify)

Un test de snapshot escribe la salida de una función en un archivo, versiona ese archivo y falla cuando la salida cambia. Sirve para resultados grandes o estructurados que serían tediosos de afirmar campo por campo: reportes formateados, payloads serializados, HTML generado.

Este apéndice usa [Verify](https://github.com/VerifyTests/Verify) con MSTest. El ejemplo formatea un resumen de pedido.

## Escribe primero el test

```csharp
[TestClass]
public sealed class OrderFormatterTests : VerifyBase
{
    [TestMethod]
    public Task FormatsTheOrder()
    {
        var order = new[]
        {
            new OrderLine("Keyboard", 2, 49.95m),
            new OrderLine("Mouse", 1, 19.95m)
        };

        return Verify(OrderFormatter.Format(order));
    }
}
```

La clase hereda `VerifyBase`. Verify.MSTest 16 se fija porque funciona con MSTest 2.2.8; las versiones posteriores de Verify.MSTest requieren MSTest 3.x.

## La primera corrida

El test falla porque todavía no hay un archivo verificado. Verify escribe un `.received.txt` junto al test:

```text
Keyboard x2 @ 49.95
Mouse x1 @ 19.95
Total: 119.85
```

Lee la salida. Si es lo que quieres, renombra el archivo recibido a `OrderFormatterTests.FormatsTheOrder.verified.txt` y haz commit. Vuelve a correr el test; pasa.

Ese renombre es el paso de aprobación. Un snapshot no es una aserción mágica; es un archivo que revisaste y aceptaste.

## Cómo funciona

En cada corrida, Verify renderiza el valor y lo compara con el archivo verificado. Una diferencia falla el test y escribe el archivo recibido para inspección. El diff muestra exactamente qué líneas cambiaron.

El archivo verificado vive en el control de versiones. Un cambio en el formateador que altera la salida es un cambio de comportamiento, y el test te obliga a mirarlo. Si la nueva salida es correcta, actualiza el archivo verificado en el mismo commit que el cambio de código.

## Un cambio para revisar

Cambia la etiqueta del total:

```csharp
rows.Add("Grand total: " + total.ToString("0.00"));
```

Ejecuta el test. Falla y el archivo recibido muestra la línea nueva. Lee el diff y después apruébalo reemplazando el archivo verificado. El snapshot atrapó un cambio que un `Assert.AreEqual` único habría dejado pasar si solo revisaba la línea del producto.

## Cuándo un snapshot es la herramienta correcta

Usa un snapshot cuando:

- La salida es grande o profundamente estructurada.
- Una aserción campo por campo sería ruidosa y escondería el cambio importante.
- La salida es lo bastante estable como para que un diff tenga sentido.

No uses un snapshot como sustituto de un test de comportamiento. Un snapshot dice "la salida cambió"; no dice "el total es correcto" salvo que leas el archivo. Mantén unas pocas aserciones enfocadas para las reglas que importan y usa el snapshot para la forma.

## Refactor

Algunas configuraciones mantienen los snapshots:

- Limpia valores inestables. Un GUID, un timestamp o una ruta de máquina cambia en cada corrida. Reemplázalos por un placeholder estable antes de verificar, o el snapshot fallará para siempre.
- Mantén los snapshots pequeños. Un snapshot de cien líneas rara vez se revisa y a menudo se aprueba a ciegas.
- Pon los archivos verificados en el control de versiones y revisa sus diffs como código.
- Usa un snapshot por escenario, no un archivo enorme para toda la aplicación.
- Corre los tests en CI. Un archivo verificado faltante es un fallo, no un skip.

El ejemplo es determinista a propósito: sin reloj, sin id aleatorio, sin rutas. Eso es lo que hace útil al snapshot.

## Para cerrar

Lo que cubrimos:

- Un test de snapshot con Verify.MSTest y un archivo verificado.
- El flujo recibido/verificado y el paso de aprobación.
- Revisar un diff cuando la salida cambia.
- Cuándo encaja un snapshot y cuándo una aserción de comportamiento es más clara.
- Scrubbers, tamaño de archivo y disciplina de control de versiones.

