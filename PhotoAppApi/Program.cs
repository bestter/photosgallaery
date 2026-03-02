using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PhotoAppApi;
using PhotoAppApi.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Connexion MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Configuration CORS (Pour laisser React communiquer avec l'API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        b => b.WithOrigins("http://localhost:3000") // Port par défaut de React
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// 3. Authentification (JWT Simplifié)
var secretKey = builder.Configuration["Jwt:Key"];
if (secretKey == null)
{
    throw new NotSupportedException("La clé secrète pour JWT n'est pas définie dans appsettings.json !");
}

// 2. Configurer le service d'authentification
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Cela affichera l'erreur réelle dans ta console de debug (Ex: Token expiré, Signature invalide...)
            Console.WriteLine("Auth échouée : " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // On demande à l'API de valider la signature avec notre clé secrète
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

        // Pour un projet de développement, on désactive souvent ces deux validations.
        // En production, tu mettrais l'URL de ton API (Issuer) et de ton app React (Audience).
        ValidateIssuer = false,
        ValidateAudience = false,

        // On vérifie que le jeton n'est pas expiré (les 24h qu'on a définies)
        ValidateLifetime = true,

        // Optionnel mais recommandé : supprime le délai de grâce par défaut de 5 minutes 
        // que Microsoft ajoute à l'expiration.
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

});


builder.Services.AddLog4net();


//builder.ConfigureLogging(logBuilder =>
// {
//     logBuilder.SetMinimumLevel(LogLevel.Trace);
//     logBuilder.AddLog4Net("log4net.config");

// }).UseConsoleLifetime();

var app = builder.Build();

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // Important pour servir les images
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();