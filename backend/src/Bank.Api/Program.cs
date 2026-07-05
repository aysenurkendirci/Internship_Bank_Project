
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Bank.Infrastructure.Extensions; // Yeni eklediğimiz namespace
using Bank.Api.Security;
using Bank.Application.Abstractions.Investments;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Services;
using Bank.Infrastructure.Oracle;
using Bank.Infrastructure.Repositories;
using Bank.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// 1) Controllers & JSON Formatting
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// 2) Swagger (JWT Bearer Support)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bank API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header, Description = "JWT Authorization header."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// 3) CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod();
    });
});

// 4) Vakifbank Integration (Sadeleşmiş Kısım)
builder.Services.AddVakifbankServices(builder.Configuration);

// 5) Oracle Infrastructure
builder.Services.Configure<OracleOptions>(builder.Configuration.GetSection("Oracle"));
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<OracleExecutor>();

// 6) Core App Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

// Repositories & Business Services
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<ICardsRepository, CardsRepository>();
builder.Services.AddScoped<ITransfersRepository, TransfersRepository>();
builder.Services.AddScoped<ITransactionsRepository, TransactionsRepository>();

// 7) JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 8) Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank API V1"));
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();