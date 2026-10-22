# Preguntas de .NET

Estas son elecciones de lenguaje y framework que aparecen al escribir los capítulos. Cada respuesta nombra el intercambio y el capítulo donde se ejercita la elección.

## ¿Record, clase o struct?

Usa una clase cuando el tipo tiene identidad y estado mutable, como un servicio o una sesión. Usa un record cuando el valor es dato inmutable y dos instancias con los mismos campos deben comparar iguales. Usa un struct para un valor pequeño que se comporta como un número y es barato de copiar.

[Clases y objetos](../fundamentals/classes/classes.es.md) modela una cuenta bancaria con una clase. [Structs y records](../fundamentals/records/records.es.md) compara la copia de un struct con la igualdad por valor de un record.

## ¿Cuándo es correcto un record struct?

Cuando quieres la copia por valor de un struct y la igualdad e impresión generadas de un record. Encaja en coordenadas pequeñas, valores tipo dinero e identificadores. No encaja en un objeto grande ni en un tipo con campos de referencia mutables.

## ¿IEnumerable o IQueryable?

Usa `IEnumerable<T>` cuando la secuencia está en memoria o el proveedor es LINQ to Objects. Usa `IQueryable<T>` cuando un proveedor como EF Core debe traducir la consulta. No expongas `IQueryable` desde un repositorio salvo que quien llama esté destinado a componer consultas.

[IEnumerable](../fundamentals/enumerable/enumerable.es.md) muestra secuencias perezosas; [IQueryable](../fundamentals/queriable/queriable.es.md) muestra el árbol de expresión y el proveedor.

## ¿Task o ValueTask?

Usa `Task` por defecto. `ValueTask` evita una asignación cuando la operación suele completar de forma síncrona, pero tiene reglas más estrictas: espérala una vez, no la guardes, no la uses en paralelo. La mayoría del código de aplicación debería devolver `Task` y dejar `ValueTask` para rutas calientes de biblioteca.

[Tasks](../fundamentals/tasks/tasks.es.md) y [Async/Await](../fundamentals/async/async.es.md) usan `Task` en todo el capítulo.

## ¿Por qué es peligroso async void?

Un método `async void` no se puede esperar, y una excepción lanzada dentro no tiene tarea que quien llama pueda observar. Solo sirve para manejadores de eventos. Los métodos asíncronos deberían devolver `Task` o `Task<T>`.

## ¿Excepciones o un tipo Result?

Usa excepciones para violaciones del contrato y fallos inesperados. Usa un patrón try o un valor de resultado cuando la entrada inválida es un desenlace esperado que quien llama debe manejar. [Excepciones](../fundamentals/exceptions/exceptions.es.md) usa una excepción de dominio; el [capítulo de Tasks](../fundamentals/tasks/tasks.es.md) devuelve valores en operaciones normales.

Un tipo `Result<T>` es una elección de diseño, no un requisito del lenguaje. Si el código usa uno, mantén el borde consistente; no mezcles tres estilos en la misma capa.

## ¿decimal o double?

Usa `decimal` para dinero, precios y cualquier valor que deba coincidir con aritmética en base 10. Usa `double` para medidas, geometría y cálculo científico donde el punto flotante binario es aceptable. [Decimales](../fundamentals/decimal/decimal.es.md) muestra la diferencia con `0.1 + 0.2`.

## ¿string o StringBuilder?

Usa `string` para un número fijo de concatenaciones y para valores que caben en una expresión. Usa `StringBuilder` en un bucle que agrega muchas piezas, donde la asignación repetida de cadenas sería un desperdicio. [Control de flujo](../fundamentals/control-flow/control-flow.es.md) usa un builder en el bucle de repetición.

## ¿Arreglo, List o IReadOnlyList?

Un arreglo tiene longitud fija y es el almacenamiento más simple cuando la cantidad se conoce. Una `List<T>` crece y es la colección de trabajo por defecto. Devuelve `IReadOnlyList<T>` o `IReadOnlyCollection<T>` desde una API pública cuando quienes llaman deben leer pero no mutar.

[Colecciones](../fundamentals/collections/collections.es.md) usa las tres y explica la elección.

## ¿Cuándo uso Span o Memory?

Usa `Span<T>` para una vista síncrona, solo de pila, sobre un búfer contiguo, como parsear una porción sin asignar. Usa `Memory<T>` cuando la vista debe guardarse en un campo o usarse a través de un await. Son herramientas de rendimiento; no las metas en código de aplicación sin una razón medida.

## ¿ref, in u out?

`ref` cuando el método debe mutar la variable de quien llama. `out` para el patrón try, donde el método devuelve éxito y un valor. `in` para un struct grande de solo lectura que no quieres copiar. Para valores normales, devuelve un valor. [ref](../fundamentals/ref/ref.es.md) cubre las tres.

## ¿Cuándo debería sellar una clase?

Sella una clase cuando la herencia no es parte de su diseño y quieres que el compilador prevenga la derivación accidental. También documenta la intención. No selles un tipo que es un punto de extensión deliberado; considera sellar implementaciones concretas que existen para satisfacer una interfaz.

## ¿DateOnly y TimeOnly?

Usa `DateOnly` para una fecha de calendario sin hora ni zona, como un cumpleaños o una fecha de vencimiento. Usa `TimeOnly` para una hora del día sin fecha. Usa `DateTimeOffset` cuando importan el instante y su offset. No modeles una fecha de calendario como un `DateTime` a medianoche en la zona del servidor.

## ¿with o mutación?

Usa `with` sobre un record para crear una copia modificada y dejar el original intacto. Usa una clase mutable cuando la identidad del objeto persiste a través de los cambios y otro código mantiene referencias a él. [La palabra with](../fundamentals/with/with.es.md) muestra actualizaciones no destructivas y la advertencia de copia superficial.

## ¿Debo usar var en todas partes?

Usa `var` cuando el lado derecho deja claro el tipo, como `var total = price * rate`. Escribe el tipo cuando el lector tendría que adivinarlo, sobre todo con el resultado de un método cuyo nombre no anuncia el tipo. [Declaraciones](../fundamentals/declarations/declarations.es.md) hace concreta la regla.

