# v13: Resiliencia con Polly

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v13-resilience)

Una dependencia externa falla a veces. Un reintento puede convertir un 503 transitorio en un éxito; también puede multiplicar el daño cuando el fallo es permanente. La versión 13 envuelve el notificador en una política de reintentos de Polly y prueba el presupuesto con un handler que falla a propósito.

La solución terminada está en `build-an-app/v13-resilience`.

## Cómo funciona

El endpoint llama a un adaptador `INotifier` que envía una petición HTTP. Un decorador ejecuta esa llamada mediante una política de Polly, que decide qué excepciones reintentar y cuántas veces. Mantener la política fuera de `HttpNotifier` separa la petición HTTP de la decisión de resiliencia.

## Cómo probarlo

Llama al endpoint de notificaciones por HTTP y reemplaza el transporte externo con un `HttpMessageHandler` falso. El handler devuelve una secuencia controlada de respuestas `503` y `200` y cuenta los intentos; así el test puede comprobar tanto la respuesta del endpoint como el presupuesto de reintentos sin usar un servicio real.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Notifications_RetryTransientFailures()
{
    using var api = new Api(notifierFailures: 2);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

El host de test usa un handler HTTP falso que devuelve `503` en las dos primeras llamadas y `200` después. Cuenta cada intento, incluido el primero.

## Intenta ejecutar el test

El test todavía no compila: falta `NotificationRequest`, y el helper de test `Api` no acepta `notifierFailures` ni expone `NotifierCalls`.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega el tipo de request y un handler falso al fixture de tests. `Api(notifierFailures: 2)` configura el handler para fallar dos veces y expone la cantidad de llamadas:

```csharp
public sealed record NotificationRequest(string Message);
```

```csharp
private sealed class FlakyHandler : HttpMessageHandler
{
    private readonly int _failuresBeforeSuccess;

    public FlakyHandler(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        var status = Calls <= _failuresBeforeSuccess
            ? HttpStatusCode.ServiceUnavailable
            : HttpStatusCode.OK;

        return Task.FromResult(new HttpResponseMessage(status));
    }
}
```

El test ya se ejecuta, pero `/notifications` devuelve 404 porque la aplicación todavía no tiene el endpoint de notificaciones.

## Escribe el código suficiente para que pase

Instala Polly y agrega un adaptador HTTP para la llamada externa:

```sh
dotnet add src/TaskTracker.Api package Polly --version 7.2.4
```

```csharp
public interface INotifier
{
    Task SendAsync(string message);
}

public sealed class HttpNotifier : INotifier
{
    private readonly HttpClient _client;

    public HttpNotifier(HttpClient client)
    {
        _client = client;
    }

    public async Task SendAsync(string message)
    {
        using var response = await _client.PostAsJsonAsync("/notify", new { message });
        response.EnsureSuccessStatusCode();
    }
}
```

`EnsureSuccessStatusCode` lanza `HttpRequestException` ante un 5xx. Envuelve el adaptador en un decorador de reintentos:

```csharp
public sealed class ResilientNotifier : INotifier
{
    private readonly INotifier _inner;
    private readonly IAsyncPolicy _policy;

    public ResilientNotifier(INotifier inner, IAsyncPolicy policy)
    {
        _inner = inner;
        _policy = policy;
    }

    public Task SendAsync(string message)
    {
        return _policy.ExecuteAsync(() => _inner.SendAsync(message));
    }
}
```

Registra el cliente tipado y el decorador, y después mapea el endpoint:

```csharp
builder.Services.AddHttpClient<HttpNotifier>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Notifier:BaseUrl"] ?? "http://notifier"));

builder.Services.AddScoped<INotifier>(services =>
    new ResilientNotifier(
        services.GetRequiredService<HttpNotifier>(),
        Policy.Handle<HttpRequestException>().RetryAsync(3)));
```

```csharp
app.MapPost("/notifications", async (NotificationRequest request, INotifier notifier) =>
{
    await notifier.SendAsync(request.Message);

    return Results.Accepted();
}).RequireAuthorization();
```

El host de test registra el mismo decorador con un `HttpClient` respaldado por `FlakyHandler` y empieza con el mismo límite de tres reintentos, así que no necesita un notificador real. El test pasa: los dos primeros intentos fallan con `503` y el tercero sale bien.

## Refactor

Un límite fijo en `RetryAsync` es una política didáctica. Producción necesita más criterio:

- Usa backoff exponencial con jitter para que una dependencia caída no reciba golpes de todos los clientes a la vez.
- Agrega un timeout por intento. Reintentar una llamada que se cuelga para siempre no es resiliencia.
- Agrega un circuit breaker tras fallos repetidos para que quien llama falle rápido mientras la dependencia se recupera.
- Reintenta solo fallos transitorios: timeouts, 5xx y errores de conexión. Un 400 o un error de validación no va a mejorar en el segundo intento.
- Registra el número de intento y el id de correlación. Un reintento que nadie ve es difícil de diagnosticar.
- Usa el `HttpClient` tipado de la factory; no crees uno nuevo por llamada.

Un reintento solo es seguro cuando la operación es idempotente. Mandar dos veces una notificación puede molestar a una persona; cobrar dos veces una tarjeta es inaceptable. Para llamadas no idempotentes, usa una clave de idempotencia y un contrato de deduplicación del lado del servidor.

Polly 8 reemplaza la API de políticas de la versión 7 con estrategias de resiliencia que se componen en un `ResiliencePipeline`. La API de la versión 7 sigue disponible mediante el paquete `Polly` ([guía de migración de Polly](https://github.com/App-vNext/Polly/blob/main/docs/migration-v8.md)).

## Repite con requisitos nuevos

### Rendirse después del presupuesto

```csharp
[TestMethod]
public async Task Notifications_GiveUpAfterTheRetryBudget()
{
    using var api = new Api(notifierFailures: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    Assert.AreEqual(3, api.NotifierCalls);
}
```

Con el presupuesto actual de tres reintentos, el endpoint devuelve 500, pero el handler registra cuatro llamadas y falla la aserción del conteo. Reduce a dos los reintentos, tanto en la configuración de producción como en la del host de test:

```csharp
Policy.Handle<HttpRequestException>().RetryAsync(2)
```

El test pasa con un intento inicial y dos reintentos. Diez fallos configurados superan lo que permite la política, así que el tercer fallo se propaga y el endpoint devuelve 500.

### No reintentar cuando hay éxito

```csharp
[TestMethod]
public async Task Notifications_DoNotRetrySuccess()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/notifications",
        new NotificationRequest("hello"));

    Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
    Assert.AreEqual(1, api.NotifierCalls);
}
```

Este test también pasa sin cambiar código: la política devuelve de inmediato una llamada exitosa.

## Para cerrar

Lo que cubrimos:

- Un notificador con `HttpClient` tipado detrás de una interfaz.
- Un decorador de reintentos de Polly con presupuesto acotado.
- Un handler falso que controla fallos y cuenta intentos.
- Tests de recuperación, agotamiento y camino exitoso.
- Idempotencia, backoff, timeout, circuit breaker y observabilidad como siguientes pasos.

Siguiente: [v14: CQRS con MediatR](../v14-cqrs/v14-cqrs.es.md).
