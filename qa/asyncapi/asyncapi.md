# AsyncAPI

[You can find the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/asyncapi)

OpenAPI describes an HTTP API: paths, methods, request bodies, status codes. An event has none of those. The publisher writes a message to a channel and moves on; the consumer has to guess the payload, the headers and the meaning of a missing field.

AsyncAPI is the contract for that side. This appendix writes the `TaskCreated` event that the messaging chapter publishes, validates the document with the official CLI, and names what a real broker would add.

## The contract

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

`channels` is the address space. Under `tasks/created`, `publish` describes what this application sends. A consumer would describe `subscribe` on the same channel, or on a channel it owns. The message is a reference to a component so the payload and headers are defined once.

`additionalProperties` is deliberately absent from the payload. A consumer should ignore fields it does not know, and a producer should be able to add one without breaking every reader. A schema that forbids extra properties makes every additive change a breaking change.

## Validate it

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

The document is valid. The remaining warning asks for a contact email in the `info` block; the repository does not publish one, and inventing an address would be worse than the warning. The CLI also reports that AsyncAPI 3.1 exists; this contract stays on 2.6 because that is the version the surrounding tooling targets, and a version migration is its own change.

## How it maps to the code

The messaging chapter publishes an in-process event:

```csharp
await _channel.Writer.WriteAsync(new TaskCreated(task.Id, task.Title, DateTime.UtcNow));
```

That `Channel<T>` is a queue inside one process. The contract says what a `TaskCreated` message means when the queue becomes a broker topic: the same `id`, `title` and `createdAt`, the same channel name, and a `correlationId` header that lets an operator follow one request across services.

The SignalR appendix pushes a `TaskCreated` payload to connected clients. That is a notification, not a durable event; AsyncAPI still describes the shape, but the delivery guarantee is different and belongs in the description, not in the schema.

## Evolving the contract

- Adding an optional field is backward compatible. A consumer that does not read it continues to work.
- Removing a field, renaming one or changing its type is a breaking change. Publish a new version of the channel or the message.
- `required` is a promise to the consumer. Adding a required field breaks every producer that has not been updated; make new fields optional first.
- A timestamp is ISO 8601 in UTC. A local time without an offset is ambiguous twice a year.
- An event id and a correlation id are different. The first identifies the message; the second ties a group of messages to one request.
- Treat the schema as a compatibility surface. A schema registry can reject an incompatible producer before it reaches consumers.

## What is deferred

There is no broker, no producer test against a real topic, no generated documentation site and no compatibility gate in CI. A hands-on lab would:

- Run RabbitMQ or Kafka in Docker and publish the event with a real client.
- Generate HTML documentation with the AsyncAPI CLI and publish it next to the OpenAPI page.
- Add a schema registry or a compatibility check that fails when a required field is removed.
- Write a consumer test that deserializes an older payload, with an extra field, and proves it still works.

## Wrapping up

What we have covered:

- An AsyncAPI 2.6 document with a channel, a message, headers and a JSON schema.
- Validation with the official CLI and the meaning of its remaining warning.
- The mapping from an in-process `Channel<T>` to a broker topic.
- Additive versus breaking changes, required fields and timestamp formats.
- The broker, docs generation and compatibility gate that a production setup adds.

Next: streaming.
