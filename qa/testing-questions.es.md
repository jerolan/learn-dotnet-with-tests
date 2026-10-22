# Preguntas de testing

Estas son las preguntas que aparecen después de los primeros capítulos: qué test escribir, qué reemplazar y cuándo un test en rojo significa que el código está mal. Cada respuesta apunta al capítulo donde se demuestra la idea.

## ¿Cuál es la diferencia entre un test unitario, de integración y de aceptación?

Un test unitario comprueba un comportamiento con sus colaboradores reemplazados. Un test de integración comprueba varias piezas reales juntas, a menudo con una base de datos o un stack HTTP. Un test de aceptación comprueba una característica a través del borde que usa la persona.

Los tests de aceptación de [la Parte 2](../testing/acceptance-tests/acceptance-tests.es.md) pasan por HTTP con `WebApplicationFactory`. Los tests de repositorio de [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md) usan una base SQLite real. Los tests de handlers de [la Parte 1](../fundamentals/methods/methods.es.md) son unitarios.

Las etiquetas importan menos que la pregunta que responde cada test. Si un test te dice si la característica funciona, consérvalo aunque alguien lo llame test de integración.

## ¿Cuándo uso un mock, un fake o la cosa real?

Usa la cosa real cuando es rápida, determinista y forma parte de lo que estás probando. Usa un fake cuando la interfaz es tuya y una implementación pequeña en memoria alcanza. Usa un mock cuando la interacción en sí es el contrato, o cuando la dependencia es un borde externo que no puedes correr en el test.

[Trabajar sin mocks](../testing/working-without-mocks/working-without-mocks.es.md) muestra un fake reemplazando un repositorio. [Mocking](../fundamentals/mock/mock.es.md) muestra a Moq verificando una llamada. Los capítulos están juntos a propósito: la mayoría de los colaboradores merecen un fake, no un mock.

## ¿Cómo pruebo código asíncrono sin demoras inestables?

Controla la finalización en vez de esperar el reloj. `TaskCompletionSource<T>` deja que el test decida cuándo termina una tarea; un fake asíncrono devuelve `Task.FromResult`, `Task.FromException` o una tarea cancelada. El [capítulo de Tasks](../fundamentals/tasks/tasks.es.md) usa esa técnica para `WhenAll` y `WhenAny`.

Si un test necesita un timeout real, dale un margen generoso y afirma sobre una condición observable, no sobre un sleep fijo. El test del worker en [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md) espera una entrada de auditoría con un plazo.

## ¿Cómo pruebo código que depende del tiempo o del azar?

Pon la dependencia detrás de una interfaz e inyéctala. Un `TimeProvider` o un `IClock` pequeño deja que el test mueva el tiempo a propósito; un `IRandomSource` o un generador con semilla hace reproducible una secuencia. Después prueba el comportamiento en el momento o con la secuencia elegida.

No afirmes sobre `DateTime.UtcNow` dentro del código de producción. Un test que compite con el reloj termina fallando en una máquina de CI lenta.

## ¿Cómo mantengo los tests independientes?

Cada test debe crear su propio estado. Los drivers de la Parte 3 crean una `WebApplicationFactory` nueva, una conexión SQLite nueva o un archivo de base nuevo. El store en memoria de [v1](../build-an-app/v1-intro/v1-intro.es.md) vivía por factory por la misma razón.

Evita el estado mutable estático y las colecciones compartidas. Si un fixture es caro, resetealo entre tests en vez de ordenar los tests. Un test que solo pasa en cierto orden es un test que va a fallar en paralelo.

## ¿Qué hago cuando un test es difícil de escribir?

Toma la dificultad como feedback de diseño. Un test que necesita cinco mocks, reflexión o llamar a un método privado te está diciendo que el código tiene demasiadas responsabilidades u oculta una dependencia. Cambia el diseño: mueve la decisión a una función pura, inyecta el colaborador o divide la clase.

La [lista de refactor de la Parte 2](../testing/refactoring-checklist/refactoring-checklist.es.md) es la lista para ese cambio. Escribe el test que describe el comportamiento que quieres y después haz el cambio de diseño más pequeño que deje el test claro.

## ¿Cuánta cobertura necesito?

La suficiente para probar el comportamiento que te importa. Un número de cobertura de líneas se puede engañar con tests que ejecutan código sin afirmar nada. Los tests de este libro se eligen por comportamiento, no por un umbral de cobertura.

Usa la cobertura para encontrar ramas sin probar y después decide si cada rama es un requisito real. Un `catch` defensivo para un estado imposible puede no necesitar test; un camino de error que una persona puede provocar sí.

## ¿Cuándo cambio el test en vez del código?

Cambia el test cuando cambió el requisito. No lo cambies porque la implementación cambió y la aserción ahora es incómoda. Un test que se rompe con un renombre pero pasa con un total incorrecto está probando lo que no debe.

La [lista de refactor](../testing/refactoring-checklist/refactoring-checklist.es.md) hace concreta la regla: si un test tiene que cambiar durante un refactor, detente. Eso es un cambio de comportamiento y necesita su propio ciclo rojo-verde.

## ¿Cómo pruebo el camino de error?

Escribe primero el test del fallo y después haz que el código de producción produzca el fallo que especificaste. [Excepciones](../fundamentals/exceptions/exceptions.es.md) usa `Assert.ThrowsException` para una excepción de dominio; el [capítulo de validación](../build-an-app/v6-validation/v6-validation.es.md) revisa el mapa `errors` de una respuesta Problem Details. Afirma el fallo y el estado que dejó.

## ¿Debo probar los métodos privados?

No. Un método privado es un detalle de implementación. Pruébalo a través del comportamiento público que lo usa. Si el método privado es demasiado complejo para probarlo indirectamente, extráelo a un tipo pequeño público o interno con un nombre y prueba ese tipo. El test debe sobrevivir a un cambio en cómo se organiza el ayudante privado.

## ¿Cómo pruebo un endpoint HTTP?

Usa `WebApplicationFactory<Program>` y un `HttpClient` real contra el servidor en memoria. Manda la petición, afirma el estado y el cuerpo, y comprueba el estado en la siguiente petición. [Tests de aceptación](../testing/acceptance-tests/acceptance-tests.es.md) muestra la primera versión; [Escalar tests de aceptación](../testing/scaling-acceptance-tests/scaling-acceptance-tests.es.md) agrega un driver y aislamiento por test.

No mockees `HttpContext` ni el delegate del endpoint. Un test que arma la respuesta a mano prueba el mock, no el endpoint.

## ¿Cómo pruebo EF Core y Dapper?

Usa un motor de base real. SQLite en memoria o un archivo temporal es rápido y determinista; los tests de repositorio de [v4](../build-an-app/v4-ef-core/v4-ef-core.es.md) y [v5](../build-an-app/v5-dapper/v5-dapper.es.md) lo usan. Prueba la migración contra el mismo proveedor que usa la app y afirma las filas, no el texto del SQL.

Para una base de servidor, usa un contenedor o Respawn para resetear el estado entre tests. El apéndice de herramientas de integración cubre ese intercambio.

