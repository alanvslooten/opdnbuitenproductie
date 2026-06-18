using System.Text;
using System.Text.Json.Serialization;
using KinderKompas.Api.Auth;
using KinderKompas.Application;
using KinderKompas.Application.Abstractions;
using KinderKompas.Infrastructure;
using KinderKompas.Infrastructure.Identity;
using KinderKompas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Render (en andere PaaS) geven de te gebruiken poort via de PORT-variabele; bind
// daarop als die aanwezig is. Lokaal blijft launchSettings (https/5181) gewoon gelden.
string? port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Null-velden weglaten: een niet-toegekend Oudercontact (thuis-portaal)
        // wordt zo niet eens meegestuurd in de JSON.
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();

// CORS: in productie draait de frontend op een ander origin (Render static site)
// en praat rechtstreeks met deze API. Toegestane origins via 'Cors:Origins'
// (komma-gescheiden), met de Render-client als terugval. Lokaal niet nodig
// (Vite proxyt /api naar dezelfde origin).
string[] toegestaneOrigins = (builder.Configuration["Cors:Origins"]
        ?? "https://kinderkompas-client.onrender.com")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(toegestaneOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Infrastructure: EF Core + Identity + auth-services.
builder.Services.AddInfrastructure(builder.Configuration);

// Application: use-case-validators (FluentValidation).
builder.Services.AddApplication();

// HTTP-/claim-gebonden providers (tenant + huidige gebruiker).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, ClaimsTenantProvider>();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// JWT-authenticatie.
JwtOptions jwt = builder.Configuration.GetSection(JwtOptions.Sectie).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Sectie 'Jwt' ontbreekt in de configuratie.");
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key ontbreekt. Stel deze in via user-secrets (lokaal) of Key Vault (productie).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Default-deny: elk endpoint vereist authenticatie tenzij expliciet [AllowAnonymous].
// Daarnaast één policy per capability voor fijnmazige autorisatie.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
    AutorisatieBeleid.VoegCapabilityPoliciesToe(options);
});

var app = builder.Build();

// Migraties toepassen en daarna seeden (idempotent). Zo bouwt een verse database
// (zoals op Render) zichzelf op bij de eerste start.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KinderKompasDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync(app.Environment.IsDevelopment());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Op Render termineert de proxy TLS en bereikt het verzoek de container via HTTP;
// een HTTPS-redirect zou daar tot een loop/warning leiden. Lokaal houden we hem aan.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Anonieme health-check (raakt de database niet, dus geen tenant-context nodig).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous()
    .WithName("Health");

app.Run();
