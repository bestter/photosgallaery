using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // 1. LA SERRURE : Cette ligne bloque l'accès à quiconque n'a pas le rôle "Admin" dans son jeton JWT
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // On récupère tous les utilisateurs, mais on omet le mot de passe haché pour la sécurité
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.Role
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des utilisateurs." });
            }
        }

        // PUT: api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] RoleUpdateDto request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Utilisateur introuvable." });
                }

                // Validation simple pour éviter les erreurs de frappe
                if (request.Role != "Admin" && request.Role != "User")
                {
                    return BadRequest(new { message = "Le rôle spécifié n'est pas valide." });
                }

                user.Role = request.Role;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Le rôle de l'utilisateur a été mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour du rôle." });
            }
        }

        // GET: api/admin/reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                // On joint la table ImageReports avec la table Photos pour avoir l'URL de l'image
                var reports = await _context.ImageReports
                    .Join(_context.Photos,
                          report => report.PhotoId,
                          photo => photo.Id,
                          (report, photo) => new
                          {
                              ReportId = report.Id,
                              PhotoId = photo.Id,
                              PhotoUrl = photo.Url,
                              Uploader = photo.UploaderUsername,
                              report.Reason,
                              report.ReportedAt
                          })
                    .OrderByDescending(r => r.ReportedAt)
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des signalements." });
            }
        }
    }

    // Un petit DTO pour recevoir le nouveau rôle
    public class RoleUpdateDto
    {
        public string Role { get; set; } = string.Empty;
    }
}