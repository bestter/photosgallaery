using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using System.Security.Claims;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly Logger _logger;

        public ImagesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _logger = new();
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetImage(string fileName)
        {
            _logger.Debug($"In {nameof(GetImage)} for file: {fileName}");

            if (string.IsNullOrEmpty(fileName) || fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains("..")) return BadRequest("Invalid file name.");
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest("Invalid file name.");
            var safeFileName = Path.GetFileName(fileName);
            try
            {
                // Trouver la photo en base de données pour vérifier les droits
                // ⚡ Bolt: Optimizing photo query to only fetch the necessary GroupId, drastically reducing data transfer and avoiding change tracking overhead.
                var photo = await _context.Photos.Where(p => p.FileName == safeFileName).Select(p => new { p.GroupId }).FirstOrDefaultAsync();

                if (photo == null)
                {
                    return NotFound();
                }

                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId directly from JWT claims.
                // This saves one DB query per private image/thumbnail load, drastically reducing latency and DB load.
                if (photo.GroupId.HasValue)
                {
                    var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(currentUserIdString, out int userId)) return Unauthorized();

                    bool isAdmin = User.IsInRole("Admin");
                    
                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value);
                        
                        if (!isMember)
                        {
                            return Forbid(); // Interdit
                        }
                    }
                }

                var rootPath = _env.ContentRootPath;
                var filePath = Path.Combine(rootPath, "PrivateImages", safeFileName);

                var fullRootPath = Path.GetFullPath(Path.Combine(rootPath, "PrivateImages"));
                var fullFilePath = Path.GetFullPath(filePath);
                if (!fullFilePath.StartsWith(fullRootPath + Path.DirectorySeparatorChar))
                {
                    return BadRequest("Invalid file path.");
                }

                // Extra protection against Windows-style traversal payloads on Linux
                if (safeFileName.Contains("..\\") || safeFileName.Contains("../") || safeFileName.Contains(".."))
                {
                    return BadRequest("Invalid file path.");
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                // Déterminer le content type
                var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                return PhysicalFile(filePath, contentType);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetImage)} for file: {fileName}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'image." });
            }
        }
        
        [HttpGet("thumbnails/{fileName}")]
        public async Task<IActionResult> GetThumbnail(string fileName)
        {
            _logger.Debug($"In {nameof(GetThumbnail)} for file: {fileName}");

            if (string.IsNullOrEmpty(fileName) || fileName.Contains("/") || fileName.Contains("\\") || fileName.Contains("..")) return BadRequest("Invalid file name.");
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest("Invalid file name.");
            var safeFileName = Path.GetFileName(fileName);
            try
            {
                // Même logique de sécurité que pour l'image pleine grandeur
                // ⚡ Bolt: Optimizing photo query to only fetch the necessary GroupId, drastically reducing data transfer and avoiding change tracking overhead.
                var photo = await _context.Photos.Where(p => p.FileName == safeFileName).Select(p => new { p.GroupId }).FirstOrDefaultAsync();

                if (photo == null) return NotFound();

                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId directly from JWT claims.
                if (photo.GroupId.HasValue)
                {
                    var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(currentUserIdString, out int userId)) return Unauthorized();

                    bool isAdmin = User.IsInRole("Admin");
                    
                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value);
                        
                        if (!isMember) return Forbid();
                    }
                }

                var rootPath = _env.ContentRootPath;
                var filePath = Path.Combine(rootPath, "PrivateImages", "thumbnails", safeFileName);

                var fullRootPath = Path.GetFullPath(Path.Combine(rootPath, "PrivateImages", "thumbnails"));
                var fullFilePath = Path.GetFullPath(filePath);
                if (!fullFilePath.StartsWith(fullRootPath + Path.DirectorySeparatorChar))
                {
                    return BadRequest("Invalid file path.");
                }

                if (safeFileName.Contains("..\\") || safeFileName.Contains("../") || safeFileName.Contains(".."))
                {
                    return BadRequest("Invalid file path.");
                }

                if (!System.IO.File.Exists(filePath)) return NotFound();

                var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                return PhysicalFile(filePath, contentType);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetThumbnail)} for file: {fileName}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération de la miniature." });
            }
        }
    }
}
