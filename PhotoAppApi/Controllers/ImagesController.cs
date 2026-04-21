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
            try
            {
                // Trouver la photo en base de données pour vérifier les droits
                var photo = await _context.Photos.FirstOrDefaultAsync(p => p.FileName == fileName);

                if (photo == null)
                {
                    return NotFound();
                }

                var currentUsername = User.Identity?.Name;
                
                // Si la photo appartient à un groupe, vérifier que l'utilisateur en fait partie ou est Admin
                if (photo.GroupId.HasValue)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                    if (user == null) return Unauthorized();

                    bool isAdmin = User.IsInRole("Admin");
                    
                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == user.Id && ug.GroupId == photo.GroupId.Value);
                        
                        if (!isMember)
                        {
                            return Forbid(); // Interdit
                        }
                    }
                }

                var rootPath = _env.ContentRootPath;
                var filePath = Path.Combine(rootPath, "PrivateImages", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                // Déterminer le content type
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
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
            try
            {
                // Même logique de sécurité que pour l'image pleine grandeur
                var photo = await _context.Photos.FirstOrDefaultAsync(p => p.FileName == fileName);

                if (photo == null) return NotFound();

                var currentUsername = User.Identity?.Name;
                
                if (photo.GroupId.HasValue)
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                    if (user == null) return Unauthorized();

                    bool isAdmin = User.IsInRole("Admin");
                    
                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == user.Id && ug.GroupId == photo.GroupId.Value);
                        
                        if (!isMember) return Forbid();
                    }
                }

                var rootPath = _env.ContentRootPath;
                var filePath = Path.Combine(rootPath, "PrivateImages", "thumbnails", fileName);

                if (!System.IO.File.Exists(filePath)) return NotFound();

                var ext = Path.GetExtension(fileName).ToLowerInvariant();
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
