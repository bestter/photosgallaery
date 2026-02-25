using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.Reflection;

namespace PhotoAppApi.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private Logger _logger;

        public PhotosController(AppDbContext context, IWebHostEnvironment env)
        {
            _logger = new();
            _context = context;
            _env = env;
        }

        // GET: api/photos (Public: tout le monde peut voir)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos()
        {
            _logger.Debug($"In {nameof(GetPhotos)}");

            return await _context.Photos.OrderByDescending(p => p.UploadedAt).ToListAsync();
        }

        // POST: api/photos/upload (Privé: connectés seulement)
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            try
            {
                _logger.Debug($"In {nameof(UploadPhoto)}");

                if (file == null || file.Length == 0)
                    return BadRequest("Aucun fichier détecté.");

                // 1. Sauvegarder le fichier sur le serveur (dossier wwwroot/images)
                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(rootPath, "images");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 2. Sauvegarder les métadonnées dans MySQL
                var photo = new Photo
                {
                    FileName = uniqueFileName,
                    Url = $"{Request.Scheme}://{Request.Host}/images/{uniqueFileName}",
                    UploaderUsername = User.Identity?.Name ?? "Anonyme"
                };

                _context.Photos.Add(photo);
                await _context.SaveChangesAsync();

                return Ok(photo);
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(UploadPhoto)}", e);
                throw;
            }
        }
    }
}