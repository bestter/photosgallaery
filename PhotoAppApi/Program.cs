using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
// NOTE: Dans un vrai projet, utilisez une clé secrète stockée dans les variables d'environnement
var key = Encoding.ASCII.GetBytes("UneCleSecreteTresLonguePourLaSecurite12345");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Cela affichera l'erreur réelle dans ta console de debug (Ex: Token expiré, Signature invalide...)
            Console.WriteLine("Auth échouée : " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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