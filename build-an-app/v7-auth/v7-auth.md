# v7: JWT Authentication

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v7-auth)

Every endpoint so far was anonymous. Version 7 adds a token endpoint, signs a JWT with a symmetric key and protects the task routes. Requests without a valid bearer token get a 401 before they reach a handler.

The finished solution is in `build-an-app/v7-auth`.

## How it works

A JWT has a header, a claims payload and a signature. The bearer middleware validates the signature, issuer, audience and lifetime, then builds the request's `ClaimsPrincipal`. `RequireAuthorization()` rejects requests without an authenticated principal; the token endpoint remains anonymous.

`ClockSkew = TimeSpan.Zero` makes the lifetime check exact, which is useful in tests and for short-lived tokens. In production, a small skew (the default is five minutes) absorbs clock differences between servers.

Authentication answers "who are you"; authorization answers "may you do this". `RequireAuthorization()` rejects an unauthenticated request with 401. A policy or role check would reject an authenticated but unauthorized request with 403; this version only needs the first gate.

## How to test

The driver gets a token by default so existing API tests keep working. Set `authenticate: false` for the anonymous-route and token-endpoint tests:

```csharp
if (authenticate)
{
    var token = GetTokenAsync().GetAwaiter().GetResult();
    Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
}
```

The test uses `WebApplicationFactory` and asserts HTTP status codes, so it covers the middleware and route boundary together.

The synchronous token request in the driver is acceptable in test setup; production code should be async all the way.

## Write the test first

```csharp
[TestMethod]
public async Task TasksWithoutToken_ReturnUnauthorized()
{
    using var api = new Api(authenticate: false);

    var response = await api.Client.GetAsync("/tasks");

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

The driver can skip authentication for this test. It fails with `Expected:<Unauthorized>. Actual:<OK>` against the anonymous v6 API.

## Try to run the test

The v6 route is anonymous, so the request succeeds with `200 OK` instead of the expected `401 Unauthorized`.

## Write the minimal amount of code for the test to run and check the failing test output

### Add the package and the options

```sh
dotnet add src/TaskTracker.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 6.0.36
```

```csharp
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TaskTracker";

    public string Audience { get; set; } = "TaskTracker";

    public string Key { get; set; } = "development-key-for-tasktracker-0123456789";

    public int ExpiryMinutes { get; set; } = 60;

    public string DemoUser { get; set; } = "admin";

    public string DemoPassword { get; set; } = "password";
}
```

The `Jwt` section in `appsettings.json` supplies the values. The demo credentials exist so the chapter runs without an identity provider; they are not a production design.

### Configure authentication

```csharp
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
```

and add the middleware before the endpoints:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

The bearer middleware is registered, but the task routes are still anonymous, so the first test still gets 200.

## Write enough code to make it pass

### Issue a token

```csharp
app.MapPost("/auth/token", (LoginRequest request, IOptions<JwtOptions> options) =>
{
    var settings = options.Value;

    if (request.Username != settings.DemoUser || request.Password != settings.DemoPassword)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, request.Username),
        new Claim(ClaimTypes.Name, request.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        settings.Issuer,
        settings.Audience,
        claims,
        expires: DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes),
        signingCredentials: credentials);

    return Results.Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token)));
});
```

### Protect the routes

```csharp
app.MapGet("/tasks", (ITaskReader reader) => reader.All()).RequireAuthorization();

app.MapGet("/tasks/{id:int}", (int id, ITaskReader reader) =>
{
    // ...
}).RequireAuthorization();
```

Add `.RequireAuthorization()` to POST and PATCH as well. The token endpoint stays anonymous.

The anonymous request now receives 401. The token endpoint can remain public while the task routes require a valid bearer token.

## Refactor

The demo credentials and the symmetric key are teaching scaffolding. In a real service:

- Read the signing key from a secret store, not from a committed file.
- Hash passwords and use an identity provider or a user store; never compare plaintext passwords.
- Use HTTPS. A bearer token is a credential; sending it over plain HTTP exposes it.
- Keep access tokens short-lived and support rotation of the signing key.
- Do not log tokens or full `Authorization` headers.

The claims are intentionally minimal. A `sub` and a name are enough for authentication; roles or scopes belong in a policy when an endpoint needs finer authorization.

## Repeat for new requirements

### Invalid credentials

```csharp
[TestMethod]
public async Task TokenEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
{
    using var api = new Api(authenticate: false);

    var response = await api.Client.PostAsJsonAsync(
        "/auth/token",
        new LoginRequest("admin", "wrong"));

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

It passes. The token endpoint does not reveal whether the username or the password was wrong; both produce the same 401.

### A token authorizes the API

```csharp
[TestMethod]
public async Task TokenEndpoint_ReturnsATokenThatAuthorizesTheApi()
{
    using var api = new Api(authenticate: false);

    var token = await api.GetTokenAsync();
    Assert.IsFalse(string.IsNullOrWhiteSpace(token));

    api.Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var response = await api.Client.GetAsync("/tasks");

    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

Green. The test gets a token from the endpoint, attaches it and reaches the protected route.


## Wrapping up

What we have covered:

- A JWT options object and a symmetric signing key.
- `AddAuthentication`/`AddJwtBearer` with issuer, audience and lifetime validation.
- A token endpoint with demo credentials.
- `RequireAuthorization()` on the task routes and a 401 without a token.
- A test driver that authenticates by default and a raw client for the anonymous case.
- The security limits of the teaching setup.

Next: [v8: service lifetimes and a decorator](../v8-di/v8-di.md).

