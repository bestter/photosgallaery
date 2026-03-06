using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.Security.Cryptography;
using System.Text.Json;

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
            try
            {
                // Ajout de .Include(p => p.Tags) pour que le frontend reçoive les tags avec les photos
                return await _context.Photos
        .Include(p => p.Tags)
            .ThenInclude(t => t.Translations) // 👈 LA LIGNE MAGIQUE EST ICI !
        .OrderByDescending(p => p.UploadedAt)
        .ToListAsync();
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(GetPhotos)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du téléchargement." });
            }
        }

        // POST: api/photos/upload (Privé: connectés seulement)
        [Authorize(Policy = "CanUpload")]
        [HttpPost("upload")]
        [RequestSizeLimit(52428800)] // Force explicitement la limite de 50 Mo sur cette route
        public async Task<IActionResult> UploadPhotos([FromForm] List<IFormFile> files, [FromForm] string tags)
        {
            try
            {
                _logger.Debug($"In {nameof(UploadPhotos)}");

                if (files == null || files.Count == 0)
                    return BadRequest(new { message = "Aucun fichier détecté." });


                var tagNames = string.IsNullOrWhiteSpace(tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(tags) ?? new List<string>();

                // Validation : entre 1 et 12 tags
                if (tagNames.Count < 1 || tagNames.Count > 12)
                {
                    return BadRequest(new { message = "Vous devez sélectionner entre 1 et 12 tags." });
                }

                var tagsToAttach = new List<Tag>();

                foreach (var name in tagNames.Select(n => n.Trim()))
                {
                    // 1. On cherche la traduction existante
                    // Note : On utilise .Tag pour le récupérer en même temps
                    var existingTranslation = await _context.TagTranslations
                        .Include(tt => tt.Tag)
                        .FirstOrDefaultAsync(tt => tt.Name.ToLower() == name.ToLower()
                                                && tt.Language == Language.FR);

                    if (existingTranslation != null)
                    {
                        // Si le tag existe déjà, on utilise l'objet Tag associé à la traduction
                        tagsToAttach.Add(existingTranslation.Tag);
                    }
                    else
                    {
                        // 2. Création d'un nouveau Tag
                        var newTag = new Tag
                        {
                            IsActive = true,
                            IsMetaTag = false
                        };

                        // 3. Création de sa traduction française
                        var newTranslation = new TagTranslation
                        {
                            Tag = newTag, // Grâce à la modif du modèle, ceci fonctionne maintenant !
                            Name = name,
                            Language = Language.FR
                        };

                        // On ajoute les deux au contexte
                        _context.Tags.Add(newTag);
                        _context.TagTranslations.Add(newTranslation);

                        // On l'ajoute à notre liste pour la photo
                        tagsToAttach.Add(newTag);
                    }
                }

                // 1. Vérification de la taille totale avant de traiter quoi que ce soit
                long totalSize = files.Sum(f => f.Length);
                if (totalSize > 52428800)
                {
                    return BadRequest(new { message = "La taille totale des fichiers dépasse la limite de 50 Mo." });
                }

                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(rootPath, "images");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uploadedPhotos = new List<Photo>();
                var errors = new List<string>();

                // 2. Boucler sur chaque fichier envoyé
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    string fileHash;
                    using (var stream = file.OpenReadStream())
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            var hashBytes = await sha256.ComputeHashAsync(stream);
                            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        }
                    }

                    // 3. Vérifier les doublons
                    bool isDuplicate = await _context.Photos.AnyAsync(p => p.FileHash == fileHash);
                    if (isDuplicate)
                    {
                        // Au lieu de bloquer toute la requête, on note l'erreur et on passe à l'image suivante
                        errors.Add($"L'image '{file.FileName}' existe déjà dans la galerie.");
                        continue;
                    }

                    // 4. Sauvegarde physique
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // 5. Préparation des métadonnées
                    var photo = new Photo
                    {
                        FileName = uniqueFileName,
                        Url = $"/images/{uniqueFileName}",
                        UploaderUsername = User.Identity?.Name ?? "Anonyme",
                        FileHash = fileHash,
                        // On clone la liste pour que chaque photo ait sa propre instance !
                        Tags = tagsToAttach.ToList()
                    };

                    _context.Photos.Add(photo);
                    uploadedPhotos.Add(photo);
                }

                // 6. Sauvegarder dans MariaDB s'il y a eu de nouvelles images
                if (uploadedPhotos.Any())
                {
                    await _context.SaveChangesAsync();
                }

                // 7. Retourner un résumé clair au client React
                return Ok(new
                {
                    message = $"{uploadedPhotos.Count} image(s) téléversée(s) avec succès.",
                    photos = uploadedPhotos,
                    erreurs = errors // React pourra afficher la liste des fichiers refusés
                });
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(UploadPhotos)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du téléversement." });
            }
        }

        // DELETE: api/photos/{id} (Privé: connectés seulement)
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            try
            {
                _logger.Debug($"In {nameof(DeletePhoto)} with id: {id}");

                // 1. Chercher la photo dans la base de données MariaDB
                var photo = await _context.Photos.FindAsync(id);
                if (photo == null)
                {
                    return NotFound();
                }

                var currentUsername = User.Identity?.Name;
                var isAdmin = User.IsInRole("Admin");

                if (photo.UploaderUsername != currentUsername && !isAdmin)
                {
                    return Forbid(); // Retourne une erreur 403 : "Tu n'as pas le droit !"
                }

                // 2. Construire le chemin physique vers le fichier sur le serveur
                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = Path.Combine(rootPath, "images", photo.FileName);

                // 3. Supprimer le fichier physique s'il existe sur le disque dur
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.Debug($"Fichier physique supprimé: {filePath}");
                }
                else
                {
                    _logger.Debug($"Fichier introuvable sur le disque (déjà supprimé ?) : {filePath}");
                }

                // 4. Supprimer l'enregistrement de la base de données
                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync();

                _logger.Debug($"Enregistrement DB supprimé avec succès pour l'ID: {id}");

                // On retourne un code 200 (Ok) pour confirmer au frontend (React) que tout s'est bien passé
                return Ok(new { message = "Photo supprimée avec succès." });
            }
            catch (Exception e)
            {
                _logger.Error($"An error occured in {nameof(DeletePhoto)}", e);
                // Il est préférable de renvoyer un code 500 (Erreur Serveur) plutôt que de juste faire un "throw" brut
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de la suppression." });
            }
        }

        // POST: api/photos/maintenance/backfill-hashes (Privé: Admin seulement)
        // Route temporaire pour mettre à jour les anciennes images
        [Authorize]
        [HttpPost("maintenance/backfill-hashes")]
        public async Task<IActionResult> BackfillHashes()
        {
            try
            {
                _logger.Debug($"In {nameof(BackfillHashes)}");

                // 1. Récupérer toutes les photos qui n'ont pas encore de Hash
                var photosSansHash = await _context.Photos
                    .Where(p => string.IsNullOrEmpty(p.FileHash))
                    .ToListAsync();

                if (!photosSansHash.Any())
                {
                    return Ok(new { message = "Toutes les photos ont déjà un FileHash. Rien à faire !" });
                }

                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                int updatedCount = 0;
                int missingFilesCount = 0;

                using (var sha256 = SHA256.Create())
                {
                    // 2. Boucler sur chaque photo
                    foreach (var photo in photosSansHash)
                    {
                        var filePath = Path.Combine(rootPath, "images", photo.FileName);

                        // 3. Vérifier si le fichier physique existe toujours
                        if (System.IO.File.Exists(filePath))
                        {
                            // Calculer le hash
                            using (var stream = System.IO.File.OpenRead(filePath))
                            {
                                var hashBytes = await sha256.ComputeHashAsync(stream);
                                photo.FileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                            }
                            updatedCount++;
                        }
                        else
                        {
                            missingFilesCount++;
                            _logger.Warn($"Fichier introuvable pour la photo ID {photo.Id} : {filePath}");
                        }
                    }
                }

                // 4. Sauvegarder toutes les modifications d'un coup dans MariaDB
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Mise à jour terminée.",
                    photosMisesAJour = updatedCount,
                    fichiersIntrouvables = missingFilesCount
                });
            }
            catch (Exception e)
            {
                _logger.Error($"Erreur dans {nameof(BackfillHashes)}", e);
                return StatusCode(500, "Erreur interne lors de la mise à jour des empreintes.");
            }
        }

        // POST: api/photos/{id}/report (Public: Visiteurs)
        [HttpPost("{id}/report")]
        [Authorize] // 🔒 LE CADENAS EST ICI !
        public async Task<IActionResult> ReportPhoto(int id, [FromBody] ReportDto request)
        {
            try
            {
                _logger.Debug($"In {nameof(ReportPhoto)} for photo ID: {id}");

                // 1. Vérifier si l'image existe toujours
                var photo = await _context.Photos.FindAsync(id);
                if (photo == null)
                {
                    return NotFound(new { message = "L'image que vous essayez de signaler n'existe plus." });
                }

                // 2. Vérifier que la raison n'est pas vide
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { message = "La raison du signalement est obligatoire." });
                }

                // 3. Créer et sauvegarder le signalement
                var report = new ImageReport
                {
                    PhotoId = id,
                    Reason = request.Reason
                };

                _context.ImageReports.Add(report);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Signalement enregistré avec succès. Un administrateur a été notifié." });
            }
            catch (Exception e)
            {
                _logger.Error($"Erreur dans {nameof(ReportPhoto)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du signalement." });
            }
        }
    }

    public class ReportDto
    {
        public string Reason { get; set; } = string.Empty;
    }
}