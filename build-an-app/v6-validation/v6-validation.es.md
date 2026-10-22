# v6: Validación con FluentValidation

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v6-validation)

La versión 5 validaba el título con dos guards dentro del handler POST. La versión 6 mueve las reglas a un validador y devuelve un problem details estándar con errores por campo.

La solución terminada está en `build-an-app/v6-validation`.

## Cómo funciona

`AbstractValidator<T>` define reglas para un DTO de petición. `RuleFor`, `Must` y `WithMessage` describen las validaciones del título; `CascadeMode.Stop` omite la regla de longitud después de fallar el título vacío. El handler agrupa los fallos y devuelve `Results.ValidationProblem`, que usa el mapa estándar `errors`.

`IValidator<T>` viene de DI. El validador recibe `IOptions<TaskOptions>`, así usa el mismo máximo configurado que el resto de la app. `Results.ValidationProblem(errors)` escribe un `HttpValidationProblemDetails` con estado 400, un `title` estándar y el mapa `errors`.

## Cómo probarlo

Envía entrada inválida por la API HTTP y comprueba el estado y el error por campo. Este ayudante lee el mapa `errors` de la respuesta Problem Details:

```csharp
private static async Task<JsonElement> ValidationErrorsAsync(HttpResponseMessage response)
{
    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    return document.RootElement.GetProperty("errors").Clone();
}
```

## Escribe primero el test

```csharp
[TestMethod]
public async Task BlankTitle_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var errors = await ValidationErrorsAsync(response);
    Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
    Assert.AreEqual("Title is required.", titleErrors[0].GetString());
}
```

El cuerpo tiene un objeto `errors` con claves por nombre de campo. Un cliente puede resaltar el input del título sin parsear un mensaje humano.

## Intenta ejecutar el test

El handler de v5 devuelve un `ErrorResponse` plano con la propiedad `error`, no el mapa `errors` que espera el test. La petición se rechaza, pero el contrato de respuesta no coincide.

## Escribe el código mínimo para que el test compile y mira la salida

### Agrega el paquete y el validador

```sh
dotnet add src/TaskTracker.Api package FluentValidation --version 11.9.2
```

```csharp
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IOptions<TaskOptions> options)
    {
        var maxTitleLength = options.Value.MaxTitleLength;

        RuleFor(request => request.Title)
            .Cascade(CascadeMode.Stop)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title is required.")
            .Must(title => title.Trim().Length <= maxTitleLength)
            .WithMessage($"Title must be at most {maxTitleLength} characters.");
    }
}
```

Regístralo:

```csharp
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
```

El validador ya está registrado, pero el handler todavía devuelve el cuerpo de error anterior, así que el test sigue en rojo.

## Escribe el código suficiente para que pase

### Úsalo en el handler

```csharp
app.MapPost("/tasks", async (CreateTaskRequest request, ITaskWriter writer, IValidator<CreateTaskRequest> validator) =>
{
    var validation = await validator.ValidateAsync(request);

    if (!validation.IsValid)
    {
        var errors = validation.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }

    var task = writer.Add(request.Title.Trim());

    return Results.Created($"/tasks/{task.Id}", task);
});
```

Verde. El handler tiene una rama de validación y un camino feliz; las reglas viven en el validador.

Si un test sobrescribe la configuración, el validador ve ese valor. El group-by mantiene juntos los mensajes de un mismo campo.

## Refactor

Algunos bordes evitan que la validación se disperse:

- Valida en el borde de la petición. El dominio y el repositorio pueden asumir un comando válido; no repiten las reglas del título.
- Mantén un validador por tipo de request. Si dos endpoints aceptan la misma forma con reglas distintas, usa requests separados o rule sets explícitos.
- Usa `MaximumLength` para longitudes simples; el `Must` a medida existe porque la regla cuenta el título recortado, que es lo que se guarda.
- Devuelve la forma estándar de Problem Details. Los clientes y gateways entienden `errors`; una forma propia necesitaría documentación y código de cliente.

FluentValidation también puede validar opciones u objetos de dominio, pero el DTO de la petición es el lugar donde entra la entrada del usuario. Ese es el borde que protege este capítulo.

## Repite con requisitos nuevos

### El límite configurado es un error de campo

El test de v3 para un título largo ahora lee el error del campo en vez de un mensaje plano:

```csharp
var errors = await ValidationErrorsAsync(response);
Assert.IsTrue(errors.TryGetProperty("Title", out var titleErrors));
StringAssert.Contains(titleErrors[0].GetString(), "10");
```

Pasa con `maxTitleLength: 10`. El test prueba que el validador lee la opción, no un número cableado.

### El valor por defecto de appsettings

El test por defecto usa 121 caracteres y busca `120` en el error del campo:

```csharp
StringAssert.Contains(titleErrors[0].GetString(), "120");
```

Verde. El validador y el archivo de appsettings coinciden.

## Para cerrar

Lo que cubrimos:

- Un validador de FluentValidation con `RuleFor`, `Must`, `WithMessage` y `CascadeMode.Stop`.
- Un validador que lee las mismas `TaskOptions` que la app.
- `IValidator<T>` inyectado en un handler de API mínima.
- Agrupar fallos por propiedad en `Results.ValidationProblem`.
- Un mapa `errors` estándar que los clientes pueden consumir.
- Sacar la validación del handler sin perder el comportamiento testeado.

Siguiente: [v7: autenticación con JWT](../v7-auth/v7-auth.es.md).

