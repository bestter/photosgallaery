using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features; // <-- NOUVEL IMPORT REQUIS
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PhotoAppApi;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
        b => b.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// 3. Authentification (JWT Simplifié)
var secretKey = builder.Configuration["Jwt:Key"];
if (secretKey == null)
{
    throw new NotSupportedException("La clé secrète pour JWT n'est pas définie dans appsettings.json !");
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
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Auth échouée : " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            // On récupère la base de données depuis le contexte de la requête
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            // On cherche l'ID de l'utilisateur dans son jeton
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 👇 AJOUTE CETTE LIGNE POUR L'ESPIONNAGE
            Console.WriteLine($"[VIDEUR] Vérification du jeton. ID trouvé : {userIdClaim ?? "AUCUN !!"}");

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanUpload", policy =>
        policy.RequireRole("Admin", "Creator"));
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();