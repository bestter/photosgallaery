using Amazon.S3;
using log4net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Features; // <-- NOUVEL IMPORT REQUIS
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PhotoAppApi;
using PhotoAppApi.Data;
using PhotoAppApi.Helpers;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using Resend;
using System.Security.Claims;
using System.Text;
using System.Threading.Channels;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var log = LogManager.GetLogger(typeof(Program));

// Retrieve the URL from appsettings.json
var frontendUrl = builder.Configuration.GetValue<string>("FrontendUrl");

// Startup security check to avoid spending hours debugging missing configuration
if (string.IsNullOrWhiteSpace(frontendUrl))
{
    throw new InvalidOperationException("The 'FrontendUrl' configuration is missing in appsettings.json.");
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
        connectionString,
        ServerVersion.AutoDetect(connectionString),
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
if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("YOUR_JWT_SECRET_KEY") || secretKey.Length < 64)
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

            log.Warn($"Auth échouée : {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // On cherche l'ID de l'utilisateur dans son jeton
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 👇 AJOUTE CETTE LIGNE POUR L'ESPIONNAGE
            log.Info($"[VIDEUR] Vérification du jeton. ID trouvé : {userIdClaim ?? "AUCUN !!"}");

            if (int.TryParse(userIdClaim, out int userId))
            {
                var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                string cacheKey = $"UserValid_{userId}";

                if (!memoryCache.TryGetValue(cacheKey, out bool isForbidden))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    // On vérifie en direct s'il a été banni depuis sa dernière connexion
                    var user = await dbContext.Users.FindAsync(userId);
                    isForbidden = (user == null || user.Role == UserRole.Forbidden);

                    // On garde le résultat en cache pour 5 minutes
                    var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    memoryCache.Set(cacheKey, isForbidden, cacheEntryOptions);
                }

                if (isForbidden)
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

var moderationUrl = builder.Configuration["ModerationURL"];
if (!string.IsNullOrWhiteSpace(moderationUrl))
{
    builder.Services.AddHttpClient("ModerationClient", client =>
    {
        client.BaseAddress = new Uri(moderationUrl); // ou URL interne Fly
        client.Timeout = TimeSpan.FromMinutes(2);
    });
    builder.Services.AddScoped<IModerationService, ModerationService>();
}

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["ObjectStorage:ServiceUrl"] ?? "https://s3.eu-west-1.amazonaws.com",
        ForcePathStyle = true,           // Important pour R2, B2 et MinIO    
        AuthenticationRegion = builder.Configuration["ObjectStorage:Region"] ?? "us-east-1"
    };

    var accessKey = builder.Configuration["ObjectStorage:AccessKey"];
    var secretKey = builder.Configuration["ObjectStorage:SecretKey"];

    if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
    {
        return new AmazonS3Client(accessKey, secretKey, config);
    }

    return new AmazonS3Client(config);
});

builder.Services.AddDataProtection()
    .SetApplicationName("PiXelLyra");

// 2. On ajoute DataProtection juste en dessous pour utiliser R2
// On dit à ASP.NET d'utiliser ta nouvelle classe personnalisée
builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
{
    return new ConfigureOptions<KeyManagementOptions>(options =>
    {
        var s3Client = sp.GetRequiredService<IAmazonS3>();
        var bucketName = builder.Configuration["ObjectStorage:BucketName"] ?? "pixellyra";

        options.XmlRepository = new CloudflareR2XmlRepository(s3Client, bucketName);
    });
});

builder.Services.Configure<ObjectStorageOptions>(
    builder.Configuration.GetSection("ObjectStorage"));

builder.Services.AddScoped<IObjectStorageService, ObjectStorageService>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanUpload", policy =>
        policy.RequireRole("Admin", "Creator"));

builder.Services.AddMemoryCache();
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("PhotosGetLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("UploadLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("RegisterLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 3,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.AddPolicy("ReportLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.AddPolicy("InviteLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.AddPolicy("GroupRequestLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 3,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.AddPolicy("ContactLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 3,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(10)
            }));
    options.AddPolicy("ViewLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("TagsLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("LikeLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("AdminLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.AddPolicy("ImageLimiter", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                          context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

var keyManager = app.Services.GetService<IKeyManager>();

if (keyManager is IDeletableKeyManager deletableKeyManager)
{
    var utcNow = DateTimeOffset.UtcNow;
    var yearAgo = utcNow.AddYears(-1);

    if (!deletableKeyManager.DeleteKeys(key => key.ExpirationDate < yearAgo))
    {
        log.Error("Failed to delete keys.");
    }
    else
    {
        log.Info("Old keys deleted successfully.");
    }
}
else
{
    log.Warn("Key manager does not support deletion.");
}

// Middleware de sécurité pour ajouter l'en-tête X-Frame-Options et autres headers de sécurité
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.googletagmanager.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; img-src 'self' data: blob: https:; font-src 'self' data: https:; connect-src 'self' ws: wss: https:;";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//https://gemini.google.com/app/387a36e26323a68d?hl=fr
app.MapFallbackToFile("/index.html");

app.Run();

public partial class Program { }
