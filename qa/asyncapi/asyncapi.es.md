# AsyncAPI

[Encuentras el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/asyncapi)

OpenAPI describe una API HTTP: rutas, métodos, cuerpos de petición, códigos de estado. Un evento no tiene nada de eso. El publicador escribe un mensaje en un canal y sigue; el consumidor tiene que adivinar el payload, los headers y el significado de un campo ausente.

AsyncAPI es el contrato de ese lado. Este apéndice escribe el evento `TaskCreated` que publica el capítulo de mensajería, valida el documento con la CLI oficial y nombra lo que agregaría un broker real.

## El contrato

`qa/asyncapi/asyncapi.yaml`:

```yaml
asyncapi: 2.6.0
id: urn:com:learndotnet:tasktracker:events
defaultContentType: application/json

tags:
  - name: tasks
    description: Task lifecycle events.

info:
  title: TaskTracker Events
  version: 1.0.0
  description: Events published when a task changes.
  contact:
    name: Learn .NET with Tests
    url: https://github.com/jerolan/learn-dotnet-with-tests
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT

servers:
  production:
    url: broker.example.com:5672
    protocol: amqp
    description: Placeholder broker; the book sample uses an in-process channel.

channels:
  tasks/created:
    publish:
      operationId: publishTaskCreated
      summary: Announce a task that was just created.
      message:
        $ref: '#/components/messages/TaskCreated'

components:
  messages:
    TaskCreated:
      messageId: taskCreated
      name: TaskCreated
      title: Task created
      contentType: application/json
      headers:
        type: object
        properties:
          correlationId:
            type: string
            description: Ties the event to the request that produced it.
      payload:
        $ref: '#/components/schemas/TaskCreatedPayload'

  schemas:
    TaskCreatedPayload:
      type: object
      required:
        - id
        - title
        - createdAt
      properties:
        id:
          type: string
          format: uuid
        title:
          type: string
          minLength: 1
        createdAt:
          type: string
          format: date-time
```

`channels` es el espacio de direcciones. Bajo `tasks/created`, `publish` describe lo que esta aplicación envía. Un consumidor describiría `subscribe` en el mismo canal, o en uno propio. El mensaje es una referencia a un componente para definir el payload y los headers una sola vez.

`additionalProperties` está ausente del payload a propósito. Un consumidor debería ignorar los campos que no conoce, y un productor debería poder agregar uno sin romper a todos los lectores. Un esquema que prohíbe propiedades extra convierte cada cambio aditivo en un cambio incompatible.

## Valídalo

```sh
npx --yes @asyncapi/cli@2.16.10 validate asyncapi.yaml
```

```text
File asyncapi.yaml is valid but has (itself and/or referenced documents) governance issues.
Warnings
asyncapi.yaml
 13:11  warning  asyncapi-info-contact-properties  Contact object should have "name", "url" and "email" fields.

1 problem (0 errors, 1 warning, 0 infos, 0 hints)
```

El documento es válido. El warning que queda pide un email de contacto en el bloque `info`; el repositorio no publica uno, e inventar una dirección sería peor que el warning. La CLI también avisa que existe AsyncAPI 3.1; este contrato se queda en 2.6 porque es la versión que apunta el tooling que lo rodea, y migrar es un cambio aparte.

## Cómo se mapea al código

El capítulo de mensajería publica un evento en proceso:

```csharp
await _channel.Writer.WriteAsync(new TaskCreated(task.Id, task.Title, DateTime.UtcNow));
```

Ese `Channel<T>` es una cola dentro de un proceso. El contrato dice qué significa un mensaje `TaskCreated` cuando la cola se vuelve un topic de broker: los mismos `id`, `title` y `createdAt`, el mismo nombre de canal y un header `correlationId` que permite seguir una petición entre servicios.

El apéndice de SignalR empuja un payload `TaskCreated` a los clientes conectados. Eso es una notificación, no un evento durable; AsyncAPI igual describe la forma, pero la garantía de entrega es distinta y va en la descripción, no en el esquema.

## Evoluciona el contrato

- Agregar un campo opcional es compatible hacia atrás. Un consumidor que no lo lee sigue funcionando.
- Quitar un campo, renombrarlo o cambiar su tipo es un cambio incompatible. Publica una versión nueva del canal o del mensaje.
- `required` es una promesa al consumidor. Agregar un campo requerido rompe a todo productor que no se actualizó; haz los campos nuevos opcionales primero.
- Un timestamp es ISO 8601 en UTC. Una hora local sin offset es ambigua dos veces al año.
- Un id de evento y un correlation id son distintos. El primero identifica el mensaje; el segundo ata un grupo de mensajes a una petición.
- Trata el esquema como una superficie de compatibilidad. Un schema registry puede rechazar a un productor incompatible antes de que llegue a los consumidores.

## Lo que queda diferido

No hay broker, ni test del productor contra un topic real, ni sitio de documentación generado, ni gate de compatibilidad en CI. Un laboratorio práctico:

- Levantaría RabbitMQ o Kafka en Docker y publicaría el evento con un cliente real.
- Generaría documentación HTML con la CLI de AsyncAPI y la publicaría junto a la página de OpenAPI.
- Agregaría un schema registry o un chequeo que falle cuando se quita un campo requerido.
- Escribiría un test de consumidor que deserialice un payload viejo, con un campo extra, y demuestre que sigue funcionando.

## Para cerrar

Lo que cubrimos:

- Un documento AsyncAPI 2.6 con canal, mensaje, headers y esquema JSON.
- Validación con la CLI oficial y el significado del warning que queda.
- El mapeo de un `Channel<T>` en proceso a un topic de broker.
- Cambios aditivos frente a incompatibles, campos requeridos y formatos de timestamp.
- El broker, la generación de docs y el gate de compatibilidad que agrega una puesta en producción.

Sigue: streaming.
