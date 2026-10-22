# Preguntas de diseño

Los capítulos de código muestran una solución cada uno. Estas preguntas son sobre elegir entre soluciones cuando más de una es defendible.

## ¿Dónde va una regla de negocio?

Pon la regla donde no se pueda saltear. Una longitud de título que todos los llamadores deben respetar va en el validador del request en el borde y, si el dominio tiene otros llamadores, también en el tipo de dominio. Una regla que solo protege un invariante interno va en el tipo dueño del estado.

[Clases y objetos](../fundamentals/classes/classes.es.md) mantiene la regla de sobregiro dentro de la cuenta. [Validación](../build-an-app/v6-validation/v6-validation.es.md) mantiene las reglas del request en FluentValidation. La duplicación es aceptable cuando las dos reglas protegen cosas distintas.

## ¿Cuándo introduzco una interfaz?

Introdúcela en un borde que necesitas reemplazar u observar: una base de datos, una dependencia HTTP, un reloj, una cola. No crees una interfaz para cada clase. Una implementación única sin costura de test agrega ceremonia y esconde el comportamiento concreto.

[Inyección de dependencias](../fundamentals/dependency-injection/dependency-injection.es.md) introduce `IMessageSender` porque el test necesita un emisor que pueda inspeccionar. [Trabajar sin mocks](../testing/working-without-mocks/working-without-mocks.es.md) muestra el fake del otro lado de esa interfaz.

## ¿Vale la pena CQRS?

CQRS vale cuando lecturas y escrituras tienen formas, necesidades de escala o modelos genuinamente distintos. No vale solo porque el acrónimo suene arquitectónico. [v14](../build-an-app/v14-cqrs/v14-cqrs.es.md) usa handlers de comando y query sobre una base; eso es una separación por caso de uso, no CQRS completo.

CQRS completo agrega un segundo modelo, consistencia eventual y una historia de sincronización. Adóptalo cuando el lado de lectura haya superado al modelo de escritura, no antes.

## ¿Un repositorio debe devolver IQueryable?

Devuelve `IQueryable<T>` solo cuando la composición es el contrato: se espera que quien llama agregue filtros y proyecciones. Si no, devuelve una lista materializada o un DTO. Un `IQueryable` filtra el proveedor y deja que quien llama arme una consulta que no diseñaste.

[IQueryable](../fundamentals/queriable/queriable.es.md) muestra el árbol de expresión y el proveedor. [Dapper](../build-an-app/v5-dapper/v5-dapper.es.md) muestra la elección opuesta: el SQL se queda dentro del lector y quien llama recibe valores.

## ¿Cuántas capas debe tener la aplicación?

Las suficientes para que las dependencias apunten en una sola dirección y no más. Un controller, un handler de caso de uso y un adaptador suelen alcanzar. Una interfaz de repositorio que solo delega en `DbContext` y un servicio que solo delega en el repositorio agregan indirección sin una decisión.

La prueba es si una capa posee un concepto. Si borrarla mezclaría dos responsabilidades no relacionadas, consérvala. Si solo renombra un método, quítala.

## ¿Cuándo uso MediatR?

Úsalo cuando quieras un punto de despacho uniforme para casos de uso o un pipeline para comportamiento transversal. No lo uses para convertir cada método en un mensaje. [v14](../build-an-app/v14-cqrs/v14-cqrs.es.md) manda comandos y queries porque los handlers hacen explícitos los casos de uso.

Si un controller llama a un servicio y el servicio ya es claro, el mediador agrega un salto. El valor aparece cuando muchos casos de uso comparten validación, logging o comportamiento transaccional.

## ¿Cómo elijo un tiempo de vida de DI?

Scoped para una unidad de trabajo: un `DbContext`, un repositorio, un contexto de petición. Singleton para estado de proceso thread-safe: configuración, un contador, una caché. Transient para ayudantes baratos y sin estado.

El caso peligroso es un singleton que captura una dependencia scoped. [Tiempos de vida y un decorador](../build-an-app/v8-di/v8-di.es.md) hace visibles los tiempos de vida y explica la dependencia cautiva. Ante la duda, scoped es el valor por defecto más seguro para todo lo que toca estado de petición.

## ¿La validación va en el dominio o en el request?

En los dos, en niveles distintos. El validador del request rechaza entrada malformada con errores por campo. El tipo de dominio protege invariantes que deben valer sin importar quién llame. Un método de dominio no puede asumir que la capa HTTP validó algo.

[Excepciones](../fundamentals/exceptions/exceptions.es.md) pone el rango de edad en el parser de dominio. [Validación](../build-an-app/v6-validation/v6-validation.es.md) pone la forma del request en FluentValidation.

## ¿Cuándo se paga sola una caché?

Cuando las lecturas dominan a las escrituras, la lectura es cara y los datos viejos son aceptables durante una ventana conocida. La historia de invalidación es parte de la decisión: cada escritura debe limpiar todas las claves que puede afectar.

[v10](../build-an-app/v10-caching/v10-caching.es.md) muestra los decoradores de lectura y escritura. Si las reglas de invalidación no son obvias, la caché probablemente llegó temprano.

## ¿Dónde va el logging?

En los bordes: peticiones, llamadas a dependencias, jobs en segundo plano y decisiones que valga auditar. El dominio puede aceptar una abstracción cuando un evento de negocio importa, pero no debe conocer proveedores, sinks ni cadenas formateadas.

[v9](../build-an-app/v9-logging/v9-logging.es.md) loguea un evento estructurado en el endpoint y un scope de correlación en el middleware. El código de dominio de esa versión no tiene logger.

## ¿Cuándo termina un refactor?

Cuando los tests están en verde, los nombres dicen lo que hace el código y el siguiente cambio es más fácil de hacer. "Más fácil" es un juicio, no una métrica. Si una abstracción propuesta tiene una sola implementación y ningún concepto detrás, el refactor no terminó; quedó postergado.

La [lista de comprobación para refactorizar](../testing/refactoring-checklist/refactoring-checklist.es.md) es la versión corta del proceso.

## ¿Cómo mantengo testeable una aplicación?

Inyecta lo que varía, mantén las decisiones en funciones puras cuando puedas y pon la E/S detrás de un borde. Una clase que crea su propio `HttpClient` o lee un reloj estático le está pidiendo al test que use reflexión. Un constructor que recibe una interfaz deja que el test entregue un fake.

Los drivers de [la Parte 3](../build-an-app/v1-intro/v1-intro.es.md) son el resultado: cada dependencia tiene una costura y cada test puede elegir su implementación.

