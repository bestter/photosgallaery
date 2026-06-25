using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhotoAppApi.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using PhotoAppApi.Services;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("ImageLimiter")]
    public class ImagesController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ImagesController));

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly IObjectStorageService _storage;
        public ImagesController(AppDbContext context, IWebHostEnvironment env, IMemoryCache cache, IObjectStorageService storage)
        {
            _context = context;
            _env = env;
            _cache = cache;
            _storage = storage;

        }
        [HttpGet("{fileName}")]
        public Task<IActionResult> GetImage(string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileName) ||
                fileName.Contains("..") ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return Task.FromResult<IActionResult>(BadRequest("Invalid file name."));
            }

            // Extract pure file name and implicitly clear CodeQL taint.
            // Validating after extracting ensures no cross-platform bypasses occur.
            var safeFileName = Path.GetFileName(fileName.Replace("\\", "/"));
            if (fileName != safeFileName)
            {
                return Task.FromResult<IActionResult>(BadRequest("Invalid file name."));
            }

            return GetImageFileInternalAsync(safeFileName, isThumbnail: false, cancellationToken);
        }

        [HttpGet("thumbnails/{fileName}")]
        public Task<IActionResult> GetThumbnail(string fileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(fileName) ||
                fileName.Contains("..") ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return Task.FromResult<IActionResult>(BadRequest("Invalid file name."));
            }

            // Extract pure file name and implicitly clear CodeQL taint.
            // Validating after extracting ensures no cross-platform bypasses occur.
            var safeFileName = Path.GetFileName(fileName.Replace("\\", "/"));
            if (fileName != safeFileName)
            {
                return Task.FromResult<IActionResult>(BadRequest("Invalid file name."));
            }

            return GetImageFileInternalAsync(safeFileName, isThumbnail: true, cancellationToken);
        }

        private async Task<IActionResult> GetImageFileInternalAsync(string safeFileName, bool isThumbnail, CancellationToken cancellationToken)
        {
            string methodName = isThumbnail ? nameof(GetThumbnail) : nameof(GetImage);
            log.Debug($"In {methodName} for file: {safeFileName}");

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
                var filePath = isThumbnail
                    ? Path.Combine(rootPath, "PrivateImages", "thumbnails", safeFileName)
                    : Path.Combine(rootPath, "PrivateImages", safeFileName);

                var fullRootPath = isThumbnail
                    ? Path.GetFullPath(Path.Combine(rootPath, "PrivateImages", "thumbnails"))
                    : Path.GetFullPath(Path.Combine(rootPath, "PrivateImages"));

                var fullFilePath = Path.GetFullPath(filePath);

                if (!string.Equals(Path.GetDirectoryName(fullFilePath), fullRootPath, StringComparison.OrdinalIgnoreCase))
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
                    var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
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
                log.Error($"An error occured in {methodName} for file: {safeFileName}", ex);
                string errorMessage = isThumbnail
                    ? "Erreur lors de la récupération de la miniature."
                    : "Erreur lors de la récupération de l'image.";
                return StatusCode(500, new { message = errorMessage });
            }
        }


        [HttpGet("s3/{photoId}")]
        public async Task<IActionResult> GetS3Image(int photoId, [FromQuery] bool isThumb = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var photo = await _context.Photos.Select(p => new { p.Id, p.GroupId, p.Url, p.ThumbnailUrl }).FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);
                if (photo == null) return NotFound();

                if (photo.GroupId.HasValue)
                {
                    var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(currentUserIdString, out int userId)) return Unauthorized();

                    if (!User.IsInRole("Admin"))
                    {
                        bool isMember = await _context.UserGroups.AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value, cancellationToken);
                        if (!isMember) return Forbid();
                    }
                }

                string? objectKey = isThumb ? photo.ThumbnailUrl : photo.Url;
                if (string.IsNullOrEmpty(objectKey)) return NotFound();

                if (objectKey.StartsWith("/api/images/"))
                {
                    var fileName = System.IO.Path.GetFileName(objectKey);
                    objectKey = objectKey.Contains("/thumbnails/") ? $"thumbnails/{fileName}" : fileName;
                }

                var url = await _storage.GetPresignedUrlAsync(objectKey, TimeSpan.FromHours(1));
                return Redirect(url);
            }
            catch (Exception ex)
            {
                log.Error($"Error generating presigned URL for photoId: {photoId}", ex);
                return StatusCode(500, new { message = "Error generating image URL." });
            }
        }

    }
}
