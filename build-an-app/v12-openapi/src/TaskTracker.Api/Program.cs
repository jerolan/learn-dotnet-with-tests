using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskTracker.Api;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskTracker")));
builder.Services.AddMemoryCache();
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.AddScoped<DapperTaskReader>();
builder.Services.AddScoped<CountingTaskReader>(services =>
    new CountingTaskReader(
        services.GetRequiredService<DapperTaskReader>(),
        services.GetRequiredService<ReadCounter>()));
builder.Services.AddScoped<ITaskReader>(services =>
    new CachingTaskReader(
        services.GetRequiredService<CountingTaskReader>(),
        services.GetRequiredService<IMemoryCache>(),
        services.GetRequiredService<IOptions<CacheOptions>>()));
builder.Services.AddScoped<EfTaskWriter>();
builder.Services.AddScoped<ITaskWriter>(services =>
    new PublishingTaskWriter(
        new InvalidatingTaskWriter(
            services.GetRequiredService<EfTaskWriter>(),
            services.GetRequiredService<IMemoryCache>()),
        services.GetRequiredService<TaskEventQueue>()));
builder.Services.AddScoped<RequestContext>();
builder.Services.AddSingleton<AppInstance>();
builder.Services.AddSingleton<ReadCounter>();
builder.Services.AddSingleton<TaskEventQueue>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<TaskEventProcessor>();
builder.Services.AddHostedService<TaskEventConsumer>();
builder.Services.Configure<CleanupOptions>(builder.Configuration.GetSection(CleanupOptions.SectionName));
builder.Services.AddScoped<TaskCleanupJob>();
builder.Services.AddHostedService<TaskCleanupService>();
builder.Services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
builder.Services.Configure<TaskOptions>(builder.Configuration.GetSection(TaskOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v1",
        Description = "Task tracker used in the Learn .NET with Tests book."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by POST /auth/token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CorrelationIdMiddleware>();

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

app.MapGet("/tasks", (ITaskReader reader) => reader.All()).RequireAuthorization();

app.MapGet("/tasks/{id:int}", (int id, ITaskReader reader) =>
{
    var task = reader.Find(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.Ok(task);
}).RequireAuthorization();

app.MapPost("/tasks", async (CreateTaskRequest request, ITaskWriter writer, IValidator<CreateTaskRequest> validator, ILogger<Program> logger) =>
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

    logger.LogInformation(
        "Task {TaskId} created with title {Title}",
        task.Id,
        task.Title);

    return Results.Created($"/tasks/{task.Id}", task);
}).RequireAuthorization();

app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, ITaskWriter writer) =>
{
    var task = writer.MarkDone(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.NoContent();
}).RequireAuthorization();

app.MapGet("/diagnostics", (RequestContext request, AppInstance app, ReadCounter counter) =>
    new DiagnosticsResponse(app.Id, request.Id, counter.Count)).RequireAuthorization();

app.Run();

public sealed record CreateTaskRequest(string Title);

public sealed record TaskItem(int Id, string Title, bool IsDone);

public sealed record ErrorResponse(string Error);

public sealed record LoginRequest(string Username, string Password);

public sealed record TokenResponse(string AccessToken);

public sealed record DiagnosticsResponse(Guid AppId, Guid RequestId, int ReadCount);

public partial class Program
{
}

