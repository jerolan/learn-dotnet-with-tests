# gRPC

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/grpc)

Un endpoint JSON es fácil de llamar desde un navegador y fácil de leer en un log. Para una llamada entre dos servicios también repite los mismos nombres de campo en cada petición, y nada en el contrato HTTP le dice al cliente qué campos son obligatorios.

gRPC arranca de un archivo `.proto`. Las herramientas generan una clase base de servidor y un cliente a partir de ese archivo, y los mensajes viajan en binario sobre HTTP/2. Este apéndice define un servicio de tareas pequeño, lo llama a través de `WebApplicationFactory` y comprueba el camino exitoso y el estado not-found.

## El contrato

`src/TaskApi/Protos/tasks.proto`:

```proto
syntax = "proto3";

option csharp_namespace = "TaskApi.Grpc";

package tasks;

service TaskService {
  rpc Create (CreateTaskRequest) returns (TaskReply);
  rpc Get (GetTaskRequest) returns (TaskReply);
}

message CreateTaskRequest {
  string title = 1;
}

message GetTaskRequest {
  string id = 1;
}

message TaskReply {
  string id = 1;
  string title = 2;
}
```

Los números son el formato de cable. Cliente y servidor acuerdan que el campo `1` significa `id`; los nombres son para el código generado y los logs. Nunca renumere un campo que ya salió, y nunca reutilices un número con otro significado. Un campo se elimina reservando su número.

## Genera el código

El proyecto de servidor referencia `Grpc.AspNetCore` e incluye el proto:

```xml
<ItemGroup>
  <PackageReference Include="Grpc.AspNetCore" Version="2.52.0" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="Protos\tasks.proto" GrpcServices="Both" />
</ItemGroup>
```

`GrpcServices="Both"` genera la clase base del servidor y el cliente en el mismo ensamblado, que mantiene este apéndice en un solo proyecto. Un sistema real pone los mensajes y el cliente en un paquete de contratos, para que el servidor y sus llamadores versionen ese paquete juntos.

## El servicio

`src/TaskApi/TaskGrpcService.cs`:

```csharp
using System.Collections.Concurrent;
using Grpc.Core;
using TaskApi.Grpc;

namespace TaskApi;

public sealed class TaskGrpcService : TaskService.TaskServiceBase
{
    private readonly ConcurrentDictionary<Guid, string> _tasks = new();

    public override Task<TaskReply> Create(CreateTaskRequest request, ServerCallContext context)
    {
        var id = Guid.NewGuid();
        _tasks[id] = request.Title;

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = request.Title });
    }

    public override Task<TaskReply> Get(GetTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id) || !_tasks.TryGetValue(id, out var title))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Task not found."));
        }

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = title });
    }
}
```

`RpcException` con un `StatusCode` se convierte en el estado gRPC que ve el cliente. No es un 404 de HTTP en el cuerpo; el estado viaja en los trailers de HTTP/2.

## El servidor

`src/TaskApi/Program.cs`:

```csharp
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<TaskGrpcService>();

var app = builder.Build();

app.MapGrpcService<TaskGrpcService>();
app.MapGet("/", () => Results.Text("gRPC endpoint: use a gRPC client."));

app.Run();

public partial class Program
{
}
```

El registro singleton es lo que hace que el diccionario sobreviva entre llamadas. Sin él, el servicio se crea por llamada y cada petición arranca con un diccionario vacío. Un servicio real guardaría sus datos en una base; el tiempo de vida sigue importando para cualquier dependencia que sostenga.

## Escribe primero el test

`tests/TaskApi.Tests/TaskGrpcTests.cs`:

```csharp
[TestMethod]
public async Task CreatedTaskCanBeReadBack()
{
    var created = await _client.CreateAsync(new CreateTaskRequest { Title = "Write the chapter" });

    var fetched = await _client.GetAsync(new GetTaskRequest { Id = created.Id });

    Assert.AreEqual(created.Id, fetched.Id);
    Assert.AreEqual("Write the chapter", fetched.Title);
}
```

El cliente necesita un canal apuntando al servidor en memoria:

```csharp
_factory = new WebApplicationFactory<Program>();
_channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress, new GrpcChannelOptions
{
    HttpHandler = _factory.Server.CreateHandler()
});
_client = new TaskService.TaskServiceClient(_channel);
```

`CreateHandler` enruta el canal a través de `TestServer`; no se abre ningún puerto. gRPC necesita HTTP/2, y el handler en memoria lo soporta.

## Intenta correrlo

Con el proto generado pero el servicio sin mapear, las dos llamadas fallan:

```text
Grpc.Core.RpcException: Status(StatusCode="Unimplemented", Detail="Bad gRPC response. HTTP status code: 404")
```

El cliente habla gRPC; el servidor no tiene un endpoint gRPC, así que la petición cae en un 404 y el cliente reporta `Unimplemented`. Agrega `app.MapGrpcService<TaskGrpcService>()`; la falta de `AddGrpc()` falla antes, cuando el servicio no se puede construir.

Corre los tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 387 ms
```

El segundo test pide un id aleatorio y espera `StatusCode.NotFound` en el `RpcException`.

## Qué compra el contrato

- El cliente generado tiene métodos y mensajes tipados. Un campo que falta es un error de compilación, no un `null` descubierto en producción.
- El payload es binario y más pequeño que el JSON equivalente, y HTTP/2 multiplexa llamadas sobre una conexión.
- El estado es parte del protocolo. `NotFound`, `InvalidArgument`, `Unavailable` y `DeadlineExceeded` se distinguen sin inventar una forma JSON de error.
- Un deadline viaja con la llamada. Ponlo en el cliente; el servidor recibe un token de cancelación en `ServerCallContext`.

El mismo proto también describe streaming: servidor, cliente y bidireccional. Este apéndice se queda en llamadas unarias porque se mapean directo sobre la petición y la respuesta que el lector ya conoce.

## Refactor para producción

- Mueve el proto y el cliente a un paquete de contratos versionado. No dejes que cada consumidor genere su copia desde un archivo del repositorio del servidor.
- Agrega un deadline a cada llamada del cliente y pasa `context.CancellationToken` al trabajo de base de datos.
- Agrega interceptores para autenticación, logging y métricas en vez de repetir ese código en cada método.
- Deja la reflexión del servidor apagada en producción salvo que tengas una razón; expone el contrato a cualquiera que alcance el puerto.
- Corre sobre TLS. HTTP/2 sin TLS no lo soportan los navegadores y no es un default seguro entre hosts.
- Fija el tamaño máximo de mensaje a propósito; el default es un límite, no una sugerencia.
- Los health checks y el balanceo pertenecen a la plataforma; un servicio gRPC detrás de un proxy L7 común puede necesitar HTTP/2 de extremo a extremo.

## Lo que queda diferido

No hay ejemplo de streaming, ni interceptor, ni TLS, ni paquete de contratos separado. Un laboratorio práctico agregaría un método server-streaming, un cliente que cancela a mitad y un deadline que expira contra un handler deliberadamente lento.

## Para cerrar

Lo que cubrimos:

- Un contrato proto con mensajes y dos métodos unarios.
- Código de servidor y cliente generado, y por qué los números de campo son permanentes.
- Un servicio gRPC singleton que mapea not-found a `StatusCode.NotFound`.
- Un canal conectado a `TestServer`, sin abrir un puerto.
- El fallo `Unimplemented` que aparece cuando el servicio no está mapeado.
- Versionado, deadlines, interceptores, TLS y reflexión como preocupaciones de producción.

Sigue: GraphQL.
