# Snapshot testing con Verify

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/snapshot-verify)

Un test de snapshot escribe la salida de una función en un archivo, versiona ese archivo y falla cuando la salida cambia. Sirve para resultados grandes o estructurados que serían tediosos de afirmar campo por campo: reportes formateados, payloads serializados, HTML generado.

Este apéndice usa [Verify](https://github.com/VerifyTests/Verify) con MSTest. El ejemplo formatea un resumen de pedido.

## Cómo funciona

Verify renderiza el valor que devuelve un test y lo compara con un archivo `.verified` revisado. Una diferencia hace fallar el test y escribe un archivo `.received` para inspeccionarlo. El snapshot es una aserción sobre toda la salida formateada.

## Cómo probarlo

Formatea un resumen pequeño de pedido y revisa el texto generado como un todo. Conserva las aserciones campo por campo para reglas como el total y usa el snapshot para la forma de la salida. La solución del apéndice ya contiene el formateador y el paquete Verify.MSTest fijado, así que el test puede enfocarse en el flujo de revisión.

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

## Intenta ejecutar el test

Ejecuta `dotnet test`. El test falla porque todavía no hay un archivo verificado. Verify escribe un `.received.txt` junto al test:

```text
Keyboard x2 @ 49.95
Mouse x1 @ 19.95
Total: 119.85
```

## Escribe el código mínimo para que el test compile y mira la salida

No hace falta cambiar el formateador para ejecutar este test: la solución ya tiene un `OrderFormatter` determinista. Lee la salida de `.received.txt` y comprueba que representa el comportamiento esperado.

## Escribe el código suficiente para que pase

Si la salida es correcta, renombra el archivo recibido a `OrderFormatterTests.FormatsTheOrder.verified.txt` y haz commit. Vuelve a correr el test; pasa.

Ese renombre es el paso de aprobación. Un snapshot no es una aserción mágica; es un archivo que revisaste y aceptaste.

## Refactor

Algunas configuraciones mantienen los snapshots:

- Limpia valores inestables. Un GUID, un timestamp o una ruta de máquina cambia en cada corrida. Reemplázalos por un placeholder estable antes de verificar, o el snapshot fallará para siempre.
- Mantén los snapshots pequeños. Un snapshot de cien líneas rara vez se revisa y a menudo se aprueba a ciegas.
- Pon los archivos verificados en control de versiones y revisa sus diffs como código.
- Usa un snapshot por escenario, no un archivo enorme para toda la aplicación.
- Ejecuta los tests en CI. Un archivo verificado faltante es un fallo, no un skip.

El ejemplo es determinista a propósito: sin reloj, sin id aleatorio, sin rutas. Eso es lo que hace útil al snapshot.

### Cuándo un snapshot es la herramienta correcta

Usa un snapshot cuando:

- La salida es grande o profundamente estructurada.
- Una aserción campo por campo sería ruidosa y escondería el cambio importante.
- La salida es lo bastante estable como para que un diff tenga sentido.

No uses un snapshot como sustituto de un test de comportamiento. Un snapshot dice "la salida cambió"; no dice "el total es correcto" salvo que leas el archivo. Mantén unas pocas aserciones enfocadas para las reglas que importan y usa el snapshot para la forma.

## Repite con requisitos nuevos

Cambia la etiqueta del total:

```csharp
rows.Add("Grand total: " + total.ToString("0.00"));
```

Ejecuta el test. Falla y el archivo recibido muestra la línea nueva. Lee el diff; si la salida nueva es correcta, apruébala reemplazando el archivo verificado. El snapshot detecta un cambio que una aserción que solo revisara una línea de producto podría pasar por alto.

## Para cerrar

Lo que cubrimos:

- Un test de snapshot con Verify.MSTest y un archivo verificado.
- El flujo recibido/verificado y el paso de aprobación.
- Revisar un diff cuando la salida cambia.
- Cuándo encaja un snapshot y cuándo una aserción de comportamiento es más clara.
- Scrubbers, tamaño de archivo y disciplina de control de versiones.
