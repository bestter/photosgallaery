using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        private readonly Logger _logger;

        // 🛡️ Sentinel: Dummy hash to equalize response times and prevent username enumeration via timing attacks
        private static readonly string _dummyHash = BCrypt.Net.BCrypt.HashPassword("dummy_password_for_timing_attack_mitigation");

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _logger = new();
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            _logger.Debug($"In {nameof(Login)} for user: {request.Username}");
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

                // 🛡️ Sentinel: Mitigate User Enumeration Timing Attacks
                // Always verify the password against a hash so the execution time remains constant regardless of whether the user exists or not.
                string hashToVerify = user != null ? user.PasswordHash : _dummyHash;

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, hashToVerify);

                if (user == null || !isPasswordValid)
                {
                    return Unauthorized(new { message = "Identifiants incorrects." });
                }


                if (user.Role == UserRole.Forbidden)
                {
                    // On renvoie un 403 explicite pour dire "Je te reconnais, mais tu es dehors"
                    return StatusCode(403, new { message = "Accès refusé. Ce compte a été suspendu par l'administration." });
                }

                string token = CreateToken(user);

                return Ok(new { token });
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
            // 1. TRADUCTION BLINDÉE : On force C# à utiliser des lettres, peu importe ce qu'il y a dans la BD
            string roleName = user.Role.ToString();
            if (roleName == "9999") roleName = UserRole.Admin.ToString();
            else if (roleName == "1") roleName = UserRole.Creator.ToString();
            else if (roleName == "0") roleName = UserRole.User.ToString();

            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, roleName)
    };

            // Note: En production, mets cette clé dans appsettings.json !
            var secretKey = _configuration["Jwt:Key"] ?? throw new NotSupportedException("Jwt:Key configuration is missing");
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
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
            _logger.Debug($"In {nameof(Register)} for user: {request.Username}");
            try
            {
                // 1. Vérifier si l'utilisateur existe déjà
                if (_context.Users.Any(u => u.Username == request.Username))
                {
                    // C'est ici la clé : on renvoie un objet avec la propriété "message"
                    return BadRequest(new { message = "Cet usager existe déjà. Veuillez vous connecter ou utiliser un autre nom de compte." });
                }

                Guid? inviteGuid = null;
                if (!string.IsNullOrWhiteSpace(request.InviteToken))
                {
                    if (Guid.TryParse(request.InviteToken, out Guid parsedGuid))
                        inviteGuid = parsedGuid;
                    else
                        return BadRequest(new { message = "Le lien d'invitation n'est pas valide." });
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

                // 5. Associer au groupe si une invitation valide est présente
                if (inviteGuid.HasValue)
                {
                    // 1. Chercher une invitation personnelle d'abord
                    var personalInvite = await _context.GroupInvitations.FirstOrDefaultAsync(i => i.InviteToken == inviteGuid.Value && i.Status == "Pending");
                    if (personalInvite != null)
                    {
                        var userGroup = new UserGroup { UserId = user.Id, GroupId = personalInvite.GroupId };
                        _context.UserGroups.Add(userGroup);
                        personalInvite.Status = "Accepted"; // Marquer comme acceptée
                        await _context.SaveChangesAsync();
                        _logger.Info($"Usager {user.Username} ajouté au groupe {personalInvite.GroupId} via INVITATION PERSONNELLE.");
                    }
                    else
                    {
                        // 2. Sinon, chercher si c'est un lien d'invitation de groupe général
                        var group = await _context.Groups.FirstOrDefaultAsync(g => g.InviteToken == inviteGuid.Value);
                        if (group != null)
                        {
                            var userGroup = new UserGroup
                            {
                                UserId = user.Id,
                                GroupId = group.Id
                            };
                            _context.UserGroups.Add(userGroup);
                            await _context.SaveChangesAsync();
                            _logger.Info($"Usager {user.Username} ajouté au groupe {group.Name} via invitation générale.");
                        }
                    }
                }

                return Ok("Compte créé avec succès !");
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(Register)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de l'enregistrement." });
            }
        }

        [HttpGet("groups")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetUserGroups()
        {
            _logger.Debug($"In {nameof(GetUserGroups)}");
            try
            {
                var username = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null) return Unauthorized();

                var groups = await _context.UserGroups
                    .Where(ug => ug.UserId == user.Id)
                    .Include(ug => ug.Group)
                    .Select(ug => new
                    {
                        ug.Group.Id,
                        ug.Group.Name,
                        ug.Group.ShortName,
                        ug.Group.InviteToken
                    })
                    .ToListAsync();

                // Si Admin, retourner tous les groupes
                if (User.IsInRole("Admin"))
                {
                    groups = await _context.Groups
                       .Select(g => new
                       {
                           g.Id,
                           g.Name,
                           g.ShortName,
                           g.InviteToken
                       })
                       .ToListAsync();
                }

                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur GetUserGroups", ex);
                return StatusCode(500, "Erreur lors de la récupération des groupes");
            }
        }
    }



    public class LoginModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}