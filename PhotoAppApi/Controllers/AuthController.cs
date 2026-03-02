using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhotoAppApi.Data;
using PhotoAppApi.dtos;
using PhotoAppApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        // 1. On déclare une variable pour stocker la configuration
        private readonly IConfiguration _configuration;

        private Logger _logger;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _logger = new();
            _context = context;
            _configuration = configuration;
        }

        // ATTENTION : Doit être exactement la même clé que dans Program.cs
        private readonly string _jwtKey = "UneCleSecreteTresLonguePourLaSecurite12345";

        [HttpPost("login")]        
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return BadRequest(new { message = "Utilisateur non trouvé." });
                }

                // 2. Si ton application plante ici, c'est que user.PasswordHash n'est pas un hash valide en base de données !
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    // 3. Changement ici : uniformisation du format JSON
                    return BadRequest(new { message = "Mot de passe incorrect." });
                }

                string token = CreateToken(user);

                return Ok(new { token = token });
            }
            catch (Exception e)
            {
                // Note: Assure-toi que _logger utilise bien .LogError() si tu utilises le logger par défaut de Microsoft
                _logger.Error($"An error occured in {nameof(Login)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de la connexion." });
            }
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

            // Note: En production, mets cette clé dans appsettings.json !
            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ma_super_cle_secrete_de_64_caracteres_minimum_123!"));
            var secretKey = _configuration["Jwt:Key"];
            if (secretKey == null)
            {
                throw new NotSupportedException("Jwt:Key configuration is missing");
            }
            SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);


            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Le jeton expire après 24h
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            try
            {
                // 1. Vérifier si l'utilisateur existe déjà
                if (_context.Users.Any(u => u.Username == request.Username))
                {
                    // C'est ici la clé : on renvoie un objet avec la propriété "message"
                    return BadRequest(new { message = "Cet usager existe déjà. Veuillez vous connecter ou utiliser un autre nom de compte." });
                }
                

                // 2. Hasher le mot de passe avec BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 3. Créer l'objet utilisateur
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = passwordHash
                };

                // 4. Sauvegarder dans MariaDB
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok("Compte créé avec succès !");
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(Register)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de l'enregistrement." });
            }
        }
    }

    public class LoginModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}