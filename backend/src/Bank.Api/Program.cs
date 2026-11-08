using System.Text;
using System.Text.Json;
using Bank.Api.ErrorHandling;
using Bank.Api.Health;
using Bank.Api.Logging;
using Bank.Api.Security;
using Bank.Api.Serialization;
using Bank.Application;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Structured logs: in production every line is one JSON object (with CorrelationId, UserId and so on as fields)
// that a log store can index; in development they stay readable in the console.
builder.Host.UseSerilog((context, services, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsDevelopment())
        logger.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
    else
        logger.WriteTo.Console(new CompactJsonFormatter());
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());

        // Malformed JSON otherwise echoes internal type names and byte offsets back to the caller.
        options.AllowInputFormatterExceptionMessages = false;
    });

// Error responses follow RFC 9457 (application/problem+json)
// Error responses carry the correlation id, so a user can quote it to support.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthRateLimiting(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // A token outlives the account it was issued for, so an erased user's token is refused here.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var erasure = context.HttpContext.RequestServices.GetRequiredService<IErasureRepository>();

                if (long.TryParse(userId, out var id) && await erasure.IsErasedAsync(id, context.HttpContext.RequestAborted))
                    context.Fail("The account has been erased.");
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Login yanıtındaki token'ı yapıştırın."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Behind the nginx container every request arrives from the proxy's address, which would make the per-IP rate
// limit count all users as one. With ReverseProxy:Enabled the client address is taken from X-Forwarded-For.
// Only turn it on where the API is reachable solely through the proxy, or callers could forge the header.
if (app.Configuration.GetValue<bool>("ReverseProxy:Enabled"))
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}

// The correlation id comes first so every later log line, including errors, carries it.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Registered before the rest so it also catches exceptions thrown by later middleware.
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank API V1"));
}

// Order matters: CORS must answer preflight requests before authentication rejects them.
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness answers "is the process up?" without touching dependencies, so a database outage does not get the
// container restarted; readiness answers "can it serve traffic?" and includes the database.
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.Run();
