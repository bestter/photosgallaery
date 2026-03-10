using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Seuls les admins peuvent accéder à ce contrôleur
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private Logger _logger;

        public AdminController(AppDbContext context)
        {
            _context = context;
            _logger = new();
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        // TRÈS IMPORTANT : On force la conversion en texte pour React
                        Role = u.Role.ToString()
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetAllUsers)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des utilisateurs." });
            }
        }

        // On modifie la route pour correspondre à React : /api/admin/users/5/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] RoleUpdateDto request)
        {
            // 1. Trouver l'utilisateur
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            // 2. Convertir le texte reçu de React ("Creator", "Admin") en véritable Enum C#
            if (Enum.TryParse<UserRole>(request.Role, true, out var newRoleEnum))
            {
                user.Role = newRoleEnum;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Le rôle de {user.Username} est maintenant {user.Role}." });
            }

            return BadRequest("Le rôle spécifié n'est pas valide.");
        }

        // GET: api/admin/reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            try
            {
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
                _logger.Error($"An error occured in {nameof(GetReports)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des signalements." });
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("reports/{id}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            try
            {
                var report = await _context.ImageReports.FindAsync(id);
                if (report == null)
                {
                    return NotFound();
                }

                _context.ImageReports.Remove(report);
                await _context.SaveChangesAsync();

                // Message mis à jour pour refléter l'action d'effacer/ignorer le signalement
                return Ok(new { message = "Le signalement a été ignoré et retiré de la liste." });
            }
            catch (Exception e)
            {
                _logger.Error($"Erreur dans {nameof(DeleteReport)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de l'effacement du signalement." });
            }
        }
    }



    // Le DTO est parfait ici !
    public class RoleUpdateDto
    {
        public string Role { get; set; } = string.Empty;
    }
}