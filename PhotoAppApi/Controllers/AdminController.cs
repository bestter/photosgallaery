using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Seuls les admins peuvent accéder à ce contrôleur
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("AdminLimiter")]
    public class AdminController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AdminController));

        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // 🛡️ Sentinel: Enforce maximum limits to prevent DoS via large DB queries and OOM.
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            log.Debug($"In {nameof(GetAllUsers)}");
            try
            {
                var query = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLowerInvariant();
                    query = query.Where(u =>
                        (u.Username != null && u.Username.ToLower().Contains(cleanSearch)) ||
                        (u.Email != null && u.Email.ToLower().Contains(cleanSearch))
                    );
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users.");
                return StatusCode(500, "Internal server error");
            }
        }

        // On modifie la route pour correspondre à React : /api/admin/users/5/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] RoleUpdateDto request, CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(UpdateUserRole)} with id: {id}");

            // 🛡️ Sentinel: Prevent Admin Lockout by ensuring users cannot modify their own role
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdString, out int currentUserId) && currentUserId == id)
            {
                return BadRequest("Vous ne pouvez pas modifier votre propre rôle.");
            }

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
        public async Task<IActionResult> GetReports(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // 🛡️ Sentinel: Enforce maximum limits to prevent DoS via large DB queries and OOM.
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            log.Debug($"In {nameof(GetReports)}");
            try
            {
                var query = _context.ImageReports
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
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLowerInvariant();
                    query = query.Where(r =>
                        (r.Reason != null && r.Reason.ToLower().Contains(cleanSearch)) ||
                        (r.Uploader != null && r.Uploader.ToLower().Contains(cleanSearch))
                    );
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var reports = await query
                    .OrderByDescending(r => r.ReportedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                return Ok(reports);
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetReports)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des signalements." });
            }
        }


        [HttpGet("reports/stats")]
        public async Task<IActionResult> GetReportStats(CancellationToken cancellationToken = default)
        {
            try
            {
                // ⚡ Bolt: Execute aggregate counts directly in the database to prevent in-memory transfer of large datasets.
                var total = await _context.ImageReports.CountAsync(cancellationToken);
                var processed = await _context.ImageReports.CountAsync(r => r.Status == "Processed", cancellationToken);
                var pending = total - processed;
                return Ok(new { total, pending, processed });
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetReportStats)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques." });
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
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;
    }
}
