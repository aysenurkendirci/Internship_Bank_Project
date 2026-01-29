using Bank.Infrastructure.Oracle;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Abstractions.Repositories; 
using Bank.Application.Services; 
using Bank.Infrastructure.Repositories; 
using Bank.Infrastructure.Security; 

var builder = WebApplication.CreateBuilder(args);

// 1. CORS Politikasını Ekle (Angular/Frontend erişimi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular portun
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Controller ve Swagger Servisleri
builder.Services.AddControllers(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

// 3. Oracle Yapılandırması
builder.Services.Configure<OracleOptions>(builder.Configuration.GetSection("Oracle"));
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<OracleExecutor>();

// 4. Dependency Injection (DI) Kayıtları
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

var app = builder.Build();

// 5. HTTP İstek Hattı (Middleware) Yapılandırması
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty; // localhost:5164 açıldığında direkt Swagger gelir
    });
}

// CORS politikasını aktif et
app.UseCors("AllowAngular");

// Controller rotalarını aktif eder
app.MapControllers();

app.Run();