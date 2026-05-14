using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoAppApi.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using log4net;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ImagesController));

                private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        public ImagesController(AppDbContext context, IWebHostEnvironment env, IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _cache = cache;

        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetImage(string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Invalid file name.");

            // To satisfy CodeQL, explicitly extract just the filename from the path before further validation.
            fileName = Path.GetFileName(fileName);

            // 🛡️ Sentinel: Strictly validate the fileName to prevent Path Traversal (CWE-22)
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains("..")) return BadRequest("Invalid file name.");

            var safeFileName = fileName;



            log.Debug($"In {nameof(GetImage)} for file: {safeFileName}");
            try
            {
                // Trouver la photo en base de données pour vérifier les droits
                // ⚡ Bolt: Optimizing photo query to only fetch the necessary GroupId, drastically reducing data transfer and avoiding change tracking overhead.
                var photo = await _context.Photos.Where(p => p.FileName == safeFileName).Select(p => new { p.GroupId }).FirstOrDefaultAsync(cancellationToken);

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
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value, cancellationToken);

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

                var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                try
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                    return File(fileStream, contentType);
                }
                catch (FileNotFoundException)
                {
                    return NotFound();
                }
                catch (DirectoryNotFoundException)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetImage)} for file: {safeFileName}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'image." });
            }
        }

        [HttpGet("thumbnails/{fileName}")]
        public async Task<IActionResult> GetThumbnail(string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Invalid file name.");

            // To satisfy CodeQL, explicitly extract just the filename from the path before further validation.
            fileName = Path.GetFileName(fileName);

            // 🛡️ Sentinel: Strictly validate the fileName to prevent Path Traversal (CWE-22)
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains("..")) return BadRequest("Invalid file name.");

            var safeFileName = fileName;



            log.Debug($"In {nameof(GetThumbnail)} for file: {safeFileName}");
            try
            {
                // Même logique de sécurité que pour l'image pleine grandeur
                // ⚡ Bolt: Optimizing photo query to only fetch the necessary GroupId, drastically reducing data transfer and avoiding change tracking overhead.
                var photo = await _context.Photos.Where(p => p.FileName == safeFileName).Select(p => new { p.GroupId }).FirstOrDefaultAsync(cancellationToken);

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
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value, cancellationToken);

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

                var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "image/jpeg"
                };

                try
                {
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
                    return File(fileStream, contentType);
                }
                catch (FileNotFoundException)
                {
                    return NotFound();
                }
                catch (DirectoryNotFoundException)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                log.Error($"An error occured in {nameof(GetThumbnail)} for file: {safeFileName}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération de la miniature." });
            }
        }
    }
}
