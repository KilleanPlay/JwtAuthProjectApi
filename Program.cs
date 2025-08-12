using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 🔹 HTTP ve HTTPS portlarını manuel ayarla
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // HTTP
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS aktif
    });
});

// EF Core için connection string al
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext'i servislere ekle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 🔹 CORS ekleme
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",   // React HTTP
            "https://localhost:3000"   // React HTTPS
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// JWT ayarları
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "JWT Auth API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer token kullanımı: \r\n\r\n 'Bearer' yazın ve bir boşluk bırakıp token'ınızı ekleyin. \r\n\r\nÖrnek: \"Bearer eyJhbGciOiJI...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// CORS aktif etme
app.UseCors("AllowReact");

app.UseAuthentication();
app.UseAuthorization();

// Giriş endpoint'i
app.MapPost("/login", async (LoginModel login, TokenService tokenService, AppDbContext db) =>
{
    var user = await db.Users
        .FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);

    if (user is null)
        return Results.Unauthorized();

    // Email ve Phone'u da token'a ekle
    var token = tokenService.GenerateToken(
        user.Username,
        user.Role.ToString(),
        user.Email,   // <-- eklendi
        user.Phone    // <-- eklendi
    );

    return Results.Ok(new { token });
});

// Örnek endpoint
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.RequireAuthorization();

app.MapGet("/admin-manager-only", (ClaimsPrincipal user) =>
{
    var username = user.Identity?.Name;
    return Results.Ok($"Merhaba {username}, bu sayfa sadece Admin ve Manager rollerine açık.");
})
.RequireAuthorization(policy => policy.RequireRole("Admin", "Manager"));

app.MapControllers();

app.Run();
