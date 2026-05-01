using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features; // <-- NOUVEL IMPORT REQUIS
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PhotoAppApi;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// On récupère l'URL depuis appsettings.json
var frontendUrl = builder.Configuration.GetValue<string>("FrontendUrl");

// Petite sécurité au démarrage pour éviter de chercher le bug pendant des heures
if (string.IsNullOrEmpty(frontendUrl))
{
    throw new InvalidOperationException("La configuration 'FrontendUrl' est introuvable dans appsettings.json.");
}

// --- NOUVEAU : Configuration de la limite à 50 Mo (52 428 800 octets) ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // Limite globale du serveur
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // Limite spécifique pour les formulaires multipart (fichiers)
});
// ------------------------------------------------------------------------

// 1. Connexion MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_DB_SERVER"))
{
    throw new InvalidOperationException("La chaîne de connexion à la base de données n'est pas configurée correctement. " +
                                        "Une chaîne valide doit être fournie via la configuration (ex: variable d'environnement ConnectionStrings__DefaultConnection).");
}

// Dans ton Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mySqlOptions =>
        {
            // C'est ici qu'on ajoute la résilience suggérée par l'erreur !
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        }
    ));

// 2. Configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        b => b.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// 3. Authentification (JWT Simplifié)
var secretKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("YOUR_JWT_SECRET_KEY"))
{
    throw new InvalidOperationException("La clé secrète pour JWT n'est pas configurée correctement. " +
                                        "Une clé d'au moins 64 caractères doit être fournie via la configuration (ex: variable d'environnement Jwt__Key).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/images"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Auth échouée : {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // On récupère la base de données depuis le contexte de la requête
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            // On cherche l'ID de l'utilisateur dans son jeton
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 👇 AJOUTE CETTE LIGNE POUR L'ESPIONNAGE
            logger.LogInformation("[VIDEUR] Vérification du jeton. ID trouvé : {UserIdClaim}", userIdClaim ?? "AUCUN !!");

            if (int.TryParse(userIdClaim, out int userId))
            {
                // On vérifie en direct s'il a été banni depuis sa dernière connexion
                var user = await dbContext.Users.FindAsync(userId);

                if (user == null || user.Role == UserRole.Forbidden)
                {
                    // Boum ! On invalide son bracelet JWT immédiatement.
                    // Cela va retourner une erreur 401 Unauthorized à React.
                    context.Fail("Ce compte a été suspendu par l'administration.");
                }
            }
        },
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = ClaimTypes.Role,
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanUpload", policy =>
        policy.RequireRole("Admin", "Creator"));

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


var channelOptions = new BoundedChannelOptions(10000)
{
    // Wait bloquera temporairement le POST (le ChannelWriter) au lieu de crasher par un OutOfMemory
    FullMode = BoundedChannelFullMode.Wait
};
var viewChannel = Channel.CreateBounded<PhotoViewEvent>(channelOptions);
// 2. Injecter les Read/Write en Singletons
builder.Services.AddSingleton(viewChannel.Writer);
builder.Services.AddSingleton(viewChannel.Reader);
// 3. Injecter le Service D'arrière plan (Le Worker)
builder.Services.AddHostedService<PhotoViewProcessingWorker>();
builder.Services.AddHostedService<HashCalculationBackgroundService>();


builder.Services.AddLog4net();

//4.Injection du service d'emails
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailService, MockEmailService>();
}
else
{
    builder.Services.AddTransient<IEmailService, ResendEmailService>();
}

var app = builder.Build();

// Middleware de sécurité pour ajouter l'en-tête X-Frame-Options
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//https://gemini.google.com/app/387a36e26323a68d?hl=fr
app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Autorise ton front-end React à lire les pixels des images
        // Remplace "*" par "http://localhost:3000" pour plus de sécurité si tu préfères
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", frontendUrl);

        // Optionnel mais recommandé pour éviter les problèmes de cache CORS
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
    }
});

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//https://gemini.google.com/app/387a36e26323a68d?hl=fr
app.MapFallbackToFile("/index.html");

app.Run();