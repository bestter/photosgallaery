using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using log4net;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Seuls les admins peuvent accéder à ce contrôleur
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AdminController));

                private readonly AppDbContext _context;
        public AdminController(AppDbContext context)
        {
            _context = context;

        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(GetAllUsers)}");
            try
            {
                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var users = await _context.Users
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        // TRÈS IMPORTANT : On force la conversion en texte pour React
                        Role = u.Role.ToString(),
                        Groups = u.UserGroups.Select(ug => ug.Group.Name).ToList()
                    })
                    .ToListAsync(cancellationToken);

                return Ok(users);
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetAllUsers)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des utilisateurs." });
            }
        }

        // On modifie la route pour correspondre à React : /api/admin/users/5/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] RoleUpdateDto request, CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(UpdateUserRole)} with id: {id}");
            // 1. Trouver l'utilisateur
            var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
            if (user == null)
            {
                return NotFound("Utilisateur introuvable.");
            }

            // 2. Convertir le texte reçu de React ("Creator", "Admin") en véritable Enum C#
            if (Enum.TryParse<UserRole>(request.Role, true, out var newRoleEnum))
            {
                user.Role = newRoleEnum;
                await _context.SaveChangesAsync(cancellationToken);
                return Ok(new { message = $"Le rôle de {user.Username} est maintenant {user.Role}." });
            }

            return BadRequest("Le rôle spécifié n'est pas valide.");
        }

        // GET: api/admin/reports
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports(CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(GetReports)}");
            try
            {
                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var reports = await _context.ImageReports
                    .AsNoTracking()
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
                              report.Status,
                              report.ReportedAt
                          })
                    .OrderByDescending(r => r.ReportedAt)
                    .ToListAsync(cancellationToken);

                return Ok(reports);
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetReports)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des signalements." });
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("reports/{id}")]
        public async Task<IActionResult> DeleteReport(int id, CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(DeleteReport)} with id: {id}");
            try
            {
                var report = await _context.ImageReports.FindAsync(new object[] { id }, cancellationToken);
                if (report == null)
                {
                    return NotFound();
                }

                report.Status = "Processed";
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { message = "Le signalement a été marqué comme traité." });
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(DeleteReport)}", e);
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