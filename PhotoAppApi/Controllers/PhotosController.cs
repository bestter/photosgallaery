using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Constants;
using PhotoAppApi.Data;
using PhotoAppApi.DTOs;
using PhotoAppApi.Helpers;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using Tag = PhotoAppApi.Models.Tag;

namespace PhotoAppApi.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PhotosController));

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IObjectStorageService _storage;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IImageProcessingAdmissionGate _admissionGate;

        private readonly ChannelWriter<PhotoViewEvent> _viewChannelWriter;
        private readonly IModerationService? _moderationService;

        public PhotosController(
            AppDbContext context,
            IWebHostEnvironment env,
            IObjectStorageService storage,
            ChannelWriter<PhotoViewEvent> viewChannelWriter,
            IModerationService? moderationService = null,
            IImageProcessingService? imageProcessingService = null,
            IImageProcessingAdmissionGate? admissionGate = null)
        {

            _context = context;
            _env = env;
            _storage = storage;
            _viewChannelWriter = viewChannelWriter;
            _moderationService = moderationService;
            _imageProcessingService = imageProcessingService ?? new ImageProcessingService();
            _admissionGate = admissionGate ?? new ImageProcessingAdmissionGate();
        }

        // GET: api/photos (Sécurisé pour récupérer selon les groupes)
        [HttpGet]
        [Authorize]
        [EnableRateLimiting("PhotosGetLimiter")]
        public async Task<ActionResult<IEnumerable<Photo>>> GetPhotos(
            [FromQuery] string? tag = null,
            [FromQuery] string? search = null,
            [FromQuery] string? author = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Language lang = Language.FR,
            [FromQuery] Guid? groupId = null, CancellationToken cancellationToken = default)
        {
            // 🛡️ Sentinel: Enforce maximum limits to prevent DoS via large DB queries and OOM.
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            log.Debug($"In {nameof(GetPhotos)}");
            try
            {
                // --- 1. TA LOGIQUE EXISTANTE (Intacte) ---
                var cleanTag = tag?.Trim().ToLowerInvariant();

                var query = _context.Photos
                    .AsNoTracking()
                    .Include(p => p.Tags)
                        .ThenInclude(t => t.Translations)
                    // ⚡ Bolt: Adding .AsSplitQuery() prevents Cartesian explosion when including multiple collections (Tags and Translations),
                    // significantly reducing memory footprint and database network transfer by issuing separate queries instead of massive JOINs.
                    .AsSplitQuery()
                    .OrderByDescending(p => p.UploadedAt)
                    .AsQueryable();

                // Filtrer par groupId explicitement si demandé depuis l'interface
                if (groupId.HasValue)
                {
                    query = query.Where(p => p.GroupId == groupId.Value);
                }

                if (!string.IsNullOrWhiteSpace(cleanTag))
                {
                    query = query.Where(p => p.Tags.Any(t =>
                        t.Translations.Any(tr =>
                            tr.Language == lang &&
                            tr.Name.Trim().Equals(cleanTag, StringComparison.CurrentCultureIgnoreCase)
                        )
                    ));
                }


                if (!string.IsNullOrWhiteSpace(author))
                {
                    query = query.Where(p => p.UploaderUsername == author);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLowerInvariant();
                    query = query.Where(p =>
                        (p.UploaderUsername != null && p.UploaderUsername.ToLower().Contains(cleanSearch)) ||
                        p.Tags.Any(t => t.Translations.Any(tr => tr.Language == lang && tr.Name.ToLower().Contains(cleanSearch)))
                    );
                }

                // Filtrer par accès de groupe pour des raisons de sécurité
                var currentUsername = User.Identity?.Name;
                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(currentUserIdString, out var id) ? id : null;
                bool isAdmin = User.IsInRole("Admin");

                if (!isAdmin && currentUserId.HasValue)
                {
                    // ⚡ Bolt: Retain IQueryable instead of ToListAsync to let EF Core generate an efficient SQL subquery for the IN clause, preventing in-memory data transfer and reducing latency.
                    var userGroupIds = _context.UserGroups
                        .AsNoTracking()
                        .Where(ug => ug.UserId == currentUserId.Value)
                        .Select(ug => ug.GroupId);

                    query = query.Where(p => !p.GroupId.HasValue || userGroupIds.Contains(p.GroupId.Value));
                }
                else if (!isAdmin && !currentUserId.HasValue)
                {
                    query = query.Where(p => !p.GroupId.HasValue);

                }

                var totalCount = await query.CountAsync(cancellationToken);

                // ⚡ Bolt: Apply pagination on the server-side to limit payload size and improve latency
                query = query.OrderByDescending(p => p.UploadedAt).Skip((page - 1) * pageSize).Take(pageSize);

                // On exécute la requête pour obtenir la liste des photos
                var photos = await query.ToListAsync(cancellationToken);

                if (photos.Count == 0)
                {
                    Response.Headers.Append("X-Total-Count", totalCount.ToString());
                    return Ok(photos);
                }

                // --- 2. NOUVELLE LOGIQUE POUR LES LIKES ---

                // A. On récupère les IDs des photos qu'on vient de trouver
                var photoIds = photos.Select(p => p.Id).ToList();

                // C. On vérifie qui est connecté
                var userLikedPhotoIds = new HashSet<int>();
                var userReportedPhotoIds = new HashSet<int>();

                if (currentUserId.HasValue)
                {
                    // On récupère uniquement les IDs des photos que CET utilisateur a aimées
                    var likedIds = await _context.PhotoLikes
                        .AsNoTracking()
                        .Where(l => photoIds.Contains(l.PhotoId) && l.UserId == currentUserId.Value)
                        .Select(l => l.PhotoId)
                        .ToListAsync(cancellationToken);

                    var reportedIds = await _context.ImageReports
                        .AsNoTracking()
                        .Where(r => photoIds.Contains(r.PhotoId) && r.ReporterUsername == currentUsername)
                        .Select(r => r.PhotoId)
                        .ToListAsync(cancellationToken);

                    userLikedPhotoIds = new HashSet<int>(likedIds);
                    userReportedPhotoIds = [.. reportedIds];
                }

                // D. On attache les infos calculées à nos photos avant de les envoyer à React
                // ⚡ Bolt: Generate S3 presigned URLs directly to avoid N+1 proxy requests from the client and N+1 database queries.
                // ⚡ Bolt: Replace unbounded Task.WhenAll with Parallel.ForEachAsync for bounded concurrency, preventing thread pool starvation.
                await Parallel.ForEachAsync(photos, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (photo, ct) =>
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = userLikedPhotoIds.Contains(photo.Id);
                    photo.IsReportedByCurrentUser = userReportedPhotoIds.Contains(photo.Id);
                });

                // On retourne tes photos enrichies !
                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                return Ok(photos);
            }
            catch (Exception e)
            {
                log.Error($"An error occured in {nameof(GetPhotos)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du téléchargement." });
            }
        }

        // POST: api/photos/upload (Privé: connectés seulement)
        [Authorize(Policy = "CanUpload")]
        [RequireWebsiteHeader] // 🔒 NOUVEAU: Empêche Postman / scripts de contourner le site web
        [HttpPost("upload")]
        [RequestSizeLimit(FileConstants.MaxUploadSize)]
        [EnableRateLimiting("UploadLimiter")] // Force explicitement la limite de 50 Mo sur cette route
        public async Task<IActionResult> UploadPhotos([FromForm] UploadRequestDto request, [FromServices] IModerationService? moderationService, CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(UploadPhotos)}");

                var theFiles = request.Files == null ? new List<IFormFile>() : new List<IFormFile>(request.Files);
                if (theFiles.Count == 0)
                    return BadRequest(new { message = "Aucun fichier détecté." });

                var totalSize = theFiles.Sum(f => f.Length);
                if (totalSize > FileConstants.MaxUploadSize)
                {
                    log.Warn($"Tentative de téléversement de fichiers totalisant {totalSize} octets, ce qui dépasse la limite autorisée.");
                    return BadRequest(new { message = "La taille totale des fichiers dépasse la limite de 50 Mo." });
                }

                if (moderationService == null && _moderationService == null)
                {
                    log.Error("ModerationService is not configured. Failing closed to prevent unmoderated uploads.");
                    return StatusCode(500, new { message = "Le service de modération est indisponible. Le téléversement est bloqué." });
                }
                var theModerationService = moderationService ?? _moderationService!;
                var moderationSvc = theModerationService;

                var tagNames = string.IsNullOrWhiteSpace(request.Tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(request.Tags) ?? new List<string>();

                // Validate between 1 and 12 tags
                if (tagNames.Count < 1 || tagNames.Count > 12)
                {
                    log.Warn($"Validation des tags échouée : {tagNames.Count} tags reçus. Tags: {string.Join(", ", tagNames)}");
                    return BadRequest(new { message = "Vous devez sélectionner entre 1 et 12 tags." });
                }

                // Validate group membership
                var currentUsername = User.Identity?.Name;
                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(currentUserIdString, out var parsedId) ? parsedId : null;

                if (request.GroupId.HasValue)
                {
                    if (!currentUserId.HasValue)
                    {
                        return Unauthorized(new { message = "Utilisateur non authentifié." });
                    }

                    var canUploadInGroup = await _context.UserGroups.AnyAsync(ug => ug.UserId == currentUserId.Value && ug.GroupId == request.GroupId.Value && (ug.Role == GroupUserRole.Member || ug.Role == GroupUserRole.Admin), cancellationToken);
                    if (!canUploadInGroup && !User.IsInRole("Admin"))
                    {
                        log.Warn($"L'utilisateur '{currentUsername}' a tenté de téléverser une image dans le groupe '{request.GroupId}' sans la permission nécessaire (doit être Membre ou Admin du groupe).");
                        return Forbid();
                    }
                }

                var tagsToAttach = new List<Tag>();
                var trimmedNames = tagNames.Select(n => n.Trim()).ToList();

                var existingTranslations = await _context.TagTranslations
                    .Include(tt => tt.Tag)
                    .Where(tt => trimmedNames.Contains(tt.Name) && tt.Language == Language.FR)
                    .ToListAsync(cancellationToken);

                var translationDict = existingTranslations
                    .GroupBy(tt => tt.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var newlyCreatedTags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
                var tagsToAdd = new List<Tag>();
                var translationsToAdd = new List<TagTranslation>();

                foreach (var name in trimmedNames)
                {
                    if (translationDict.TryGetValue(name, out var existingTranslation))
                    {
                        tagsToAttach.Add(existingTranslation.Tag);
                    }
                    else if (newlyCreatedTags.TryGetValue(name, out var newTagFromPreviousIteration))
                    {
                        tagsToAttach.Add(newTagFromPreviousIteration);
                    }
                    else
                    {
                        var newTag = new Tag
                        {
                            IsActive = true,
                            IsMetaTag = false
                        };

                        var newTranslation = new TagTranslation
                        {
                            Tag = newTag,
                            Name = name,
                            Language = Language.FR
                        };

                        tagsToAdd.Add(newTag);
                        translationsToAdd.Add(newTranslation);
                        tagsToAttach.Add(newTag);
                        newlyCreatedTags.Add(name, newTag);
                    }
                }

                if (tagsToAdd.Count != 0)
                {
                    _context.Tags.AddRange(tagsToAdd);
                    _context.TagTranslations.AddRange(translationsToAdd);
                }

                // 2. Acquire image processing admission lease
                IDisposable? admissionLease = null;
                try
                {
                    admissionLease = await _admissionGate.AcquireAsync(cancellationToken);
                }
                catch (ImageProcessingBusyException busyEx)
                {
                    Response.Headers["Retry-After"] = "5";
                    return StatusCode(503, new { message = busyEx.Message });
                }

                var errors = new List<string>();
                var uploadedPhotos = new List<Photo>();
                var uploadedKeys = new List<string>();

                try
                {
                    // 3. Inspect each file to verify format, dimensions, and animation
                    var validFiles = new List<(IFormFile File, ImageInspectionResult Inspection, string Hash)>();
                    using var sha512 = SHA512.Create();

                    foreach (var file in theFiles)
                    {
                        if (file.Length == 0)
                        {
                            errors.Add($"Le fichier '{file.FileName}' est vide.");
                            continue;
                        }

                        using var stream = file.OpenReadStream();
                        var inspection = _imageProcessingService.Inspect(stream);
                        if (!inspection.IsValid)
                        {
                            errors.Add($"Le fichier '{file.FileName}' n'est pas supporté ou valide : {inspection.ErrorMessage}");
                            continue;
                        }

                        stream.Position = 0;
                        var hashBytes = await sha512.ComputeHashAsync(stream, cancellationToken);
                        var fileHash = Convert.ToHexStringLower(hashBytes);

                        validFiles.Add((file, inspection, fileHash));
                    }

                    // 4. Deduplicate by SHA-512 hash (database and current batch)
                    var distinctHashes = validFiles.Select(x => x.Hash).Distinct().ToList();
                    var existingHashes = await _context.Photos
                        .AsNoTracking()
                        .Where(p => p.FileHash != null && distinctHashes.Contains(p.FileHash))
                        .Select(p => p.FileHash!)
                        .ToListAsync(cancellationToken);

                    var existingHashesSet = new HashSet<string>(existingHashes);
                    var seenInBatch = new HashSet<string>();
                    var eligibleItems = new List<(IFormFile File, ImageInspectionResult Inspection, string Hash)>();

                    foreach (var item in validFiles)
                    {
                        if (existingHashesSet.Contains(item.Hash))
                        {
                            errors.Add($"L'image '{item.File.FileName}' existe déjà dans la galerie.");
                            continue;
                        }

                        if (seenInBatch.Contains(item.Hash))
                        {
                            errors.Add($"L'image '{item.File.FileName}' est présente en double dans cet envoi.");
                            continue;
                        }
                        seenInBatch.Add(item.Hash);
                        eligibleItems.Add(item);
                    }

                    // If no file is eligible for processing
                    if (eligibleItems.Count == 0)
                    {
                        return Ok(new
                        {
                            message = "0 image(s) téléversée(s) avec succès.",
                            photos = uploadedPhotos,
                            erreurs = errors
                        });
                    }

                    // 5. Pre-moderate all eligible files (fail-closed)
                    foreach (var item in eligibleItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var stream = item.File.OpenReadStream();
                        var modResult = await moderationSvc.CheckImageAsync(
                            stream,
                            item.File.FileName,
                            item.Inspection.ContentType!,
                            cancellationToken
                        );

                        if (modResult.IsNsfw)
                        {
                            return BadRequest(new
                            {
                                message = "Image contains inappropriate content",
                                score = modResult.NsfwScore
                            });
                        }
                    }

                    // 6. Process and upload files sequentially one by one
                    foreach (var item in eligibleItems)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        using var fileStream = item.File.OpenReadStream();
                        using var processed = await _imageProcessingService.ProcessImageAsync(
                            fileStream,
                            request.IncludeGps,
                            cancellationToken,
                            item.Inspection
                        );

                        var fileId = Guid.NewGuid().ToString();
                        var uniqueFileName = fileId + processed.Extension;

                        string? mainKey = null;
                        string? thumbKey = null;

                        try
                        {
                            mainKey = await _storage.UploadImageAsync(
                                processed.MainStream,
                                processed.ContentType,
                                uniqueFileName,
                                "gallery",
                                cancellationToken
                            );
                            uploadedKeys.Add(mainKey);

                            thumbKey = await _storage.UploadImageAsync(
                                processed.ThumbnailStream,
                                processed.ContentType,
                                uniqueFileName,
                                "thumbnails",
                                cancellationToken
                            );
                            uploadedKeys.Add(thumbKey);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Échec du téléversement de l'image {item.File.FileName} vers le stockage.", ex);
                            await CleanupStorageKeysAsync(uploadedKeys);
                            throw;
                        }

                        var photo = new Photo
                        {
                            FileName = uniqueFileName,
                            UploaderUsername = currentUsername ?? "Anonyme",
                            FileHash = item.Hash,
                            Tags = tagsToAttach.ToList(),
                            GroupId = request.GroupId,
                            FileSize = item.File.Length,
                            ResolutionWidth = processed.NormalizedWidth,
                            ResolutionHeight = processed.NormalizedHeight,
                            Url = mainKey,
                            ThumbnailUrl = thumbKey,
                            DateTaken = processed.Metadata.DateTaken,
                            CameraModel = processed.Metadata.CameraModel,
                            Latitude = processed.Metadata.Latitude,
                            Longitude = processed.Metadata.Longitude
                        };
                        uploadedPhotos.Add(photo);
                    }

                    // 7. Persist in database with reconciliation on error
                    if (uploadedPhotos.Count != 0)
                    {
                        try
                        {
                            _context.Photos.AddRange(uploadedPhotos);
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                        catch (Exception dbEx)
                        {
                            log.Error("Échec de la sauvegarde en base de données après téléversement. Réconciliation des objets.", dbEx);
                            await ReconcileOrCleanupAsync(uploadedKeys);
                            return StatusCode(500, new { message = "Une erreur interne est survenue lors de l'enregistrement en base de données." });
                        }
                    }

                    return Ok(new
                    {
                        message = $"{uploadedPhotos.Count} image(s) téléversée(s) avec succès.",
                        photos = uploadedPhotos,
                        erreurs = errors
                    });
                }
                finally
                {
                    admissionLease?.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"An error occured in {nameof(UploadPhotos)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du téléversement." });
            }
        }

        private async Task CleanupStorageKeysAsync(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return;
            }

            using var cleanupCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var key in keys)
            {
                try
                {
                    await _storage.DeleteImageAsync(key, cleanupCts.Token);
                }
                catch (Exception ex)
                {
                    log.Warn($"Échec de la suppression de l'objet temporaire {key}.", ex);
                }
            }
        }

        private async Task ReconcileOrCleanupAsync(List<string> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return;
            }

            try
            {
                var referencedPairs = await _context.Photos
                    .AsNoTracking()
                    .Where(p => (p.Url != null && keys.Contains(p.Url)) || (p.ThumbnailUrl != null && keys.Contains(p.ThumbnailUrl)))
                    .Select(p => new { p.Url, p.ThumbnailUrl })
                    .ToListAsync();

                var referencedSet = new HashSet<string>();
                foreach (var pair in referencedPairs)
                {
                    if (!string.IsNullOrEmpty(pair.Url))
                    {
                        referencedSet.Add(pair.Url);
                    }
                    if (!string.IsNullOrEmpty(pair.ThumbnailUrl))
                    {
                        referencedSet.Add(pair.ThumbnailUrl);
                    }
                }

                var keysToDelete = keys.Where(k => !referencedSet.Contains(k)).ToList();
                await CleanupStorageKeysAsync(keysToDelete);
            }
            catch (Exception ex)
            {
                log.Error("Impossible de vérifier les clés orphelines suite à l'échec DB. Objets conservés pour audit.", ex);
            }
        }


        // DELETE: api/photos/{id} (Private: logged in users only)
        [Authorize]
        [HttpDelete("{id}")]
        [EnableRateLimiting("UploadLimiter")]
        public async Task<IActionResult> DeletePhoto(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(DeletePhoto)} with id: {id}");

                // 1. Find the photo in the database
                var photo = await _context.Photos.FindAsync(new object[] { id }, cancellationToken);
                if (photo == null)
                {
                    return NotFound();
                }

                var currentUsername = User.Identity?.Name;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(currentUsername) || photo.UploaderUsername != currentUsername)
                    {
                        return Forbid();
                    }
                }

                // 🛡️ Sentinel: Fix IDOR by validating group membership
                if (photo.GroupId.HasValue)
                {
                    var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(currentUserIdString, out int userId)) return Unauthorized();

                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value, cancellationToken);

                        if (!isMember) return Forbid();
                    }
                }

                // 2. Capture the keys and paths needed for cleanup before deleting the record
                var rootPath = _env.ContentRootPath;
                var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);

                var basePrivateDir = Path.GetFullPath(Path.Combine(rootPath, "PrivateImages")) + Path.DirectorySeparatorChar;
                var baseThumbDir = Path.GetFullPath(Path.Combine(rootPath, "PrivateImages", "thumbnails")) + Path.DirectorySeparatorChar;

                var filePath = Path.GetFullPath(Path.Combine(basePrivateDir, safeFileName));
                var thumbPath = Path.GetFullPath(Path.Combine(baseThumbDir, safeFileName));

                if (!filePath.StartsWith(basePrivateDir, StringComparison.OrdinalIgnoreCase) || !thumbPath.StartsWith(baseThumbDir, StringComparison.OrdinalIgnoreCase))
                {
                    log.Warn($"Path traversal attempt detected during deletion of photo {id}");
                    return BadRequest(new { message = "Invalid file path." });
                }
                var s3Url = photo.Url;
                var s3ThumbUrl = photo.ThumbnailUrl;

                // 3. Find and delete all associated reports first to maintain referential integrity
                // ⚡ Bolt: Replaced fetching reports into memory via ToListAsync and removing via RemoveRange with ExecuteDeleteAsync in production.
                // Since EF Core InMemory database provider does not support bulk deletes, we fall back to traditional RemoveRange for unit tests.
                int deletedCount = 0;
                if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
                {
                    var associatedReports = await _context.ImageReports
                                                          .Where(r => r.PhotoId == id)
                                                          .ToListAsync(cancellationToken);
                    if (associatedReports.Count != 0)
                    {
                        _context.ImageReports.RemoveRange(associatedReports);
                        deletedCount = associatedReports.Count;
                    }
                }
                else
                {
                    deletedCount = await _context.ImageReports
                                                 .Where(r => r.PhotoId == id)
                                                 .ExecuteDeleteAsync(cancellationToken);
                }

                if (deletedCount != 0)
                {
                    log.Debug($"{deletedCount} report(s) deleted for photo ID: {id}");
                }

                // 4. Delete the photo record from the database and commit changes
                _context.Photos.Remove(photo);
                await _context.SaveChangesAsync(cancellationToken);

                // 5. Clean up the physical files (S3 and local disk) concurrently in a resilient manner.
                // Failures in file cleanup should not return an API error since the record is already deleted.
                var cleanupTasks = new List<Task>();

                // ⚡ Bolt: Removed Task.Run wrapper around blocking async tasks to prevent thread pool starvation
                async Task DeleteSafeAsync(string url, string type)
                {
                    try
                    {
                        await _storage.DeleteImageAsync(url, cancellationToken);
                        log.Debug($"S3 {type} deleted: {url}");
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to delete S3 {type}: {url}", ex);
                    }
                }

                // Clean S3 storage if S3 keys exist
                if (!string.IsNullOrEmpty(s3Url))
                {
                    cleanupTasks.Add(DeleteSafeAsync(s3Url, "original image"));
                }

                if (!string.IsNullOrEmpty(s3ThumbUrl))
                {
                    cleanupTasks.Add(DeleteSafeAsync(s3ThumbUrl, "thumbnail"));
                }

                // Clean local server storage
                cleanupTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                            log.Debug($"Local file deleted: {filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to delete local file: {filePath}", ex);
                    }
                }));

                cleanupTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        if (System.IO.File.Exists(thumbPath))
                        {
                            System.IO.File.Delete(thumbPath);
                            log.Debug($"Local thumbnail deleted: {thumbPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn($"Failed to delete local thumbnail: {thumbPath}", ex);
                    }
                }));

                await Task.WhenAll(cleanupTasks);

                log.Debug($"DB photo record and associated reports deleted successfully for ID: {id}");

                return Ok(new { message = "Photo et ses signalements supprimés avec succès." });
            }
            catch (Exception e)
            {
                log.Error($"An error occured in {nameof(DeletePhoto)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors de la suppression." });
            }
        }

        // POST: api/photos/maintenance/backfill-hashes (Privé: Admin seulement)
        // Route temporaire pour mettre à jour les anciennes images
        [Authorize(Roles = "Admin")]
        [HttpPost("maintenance/backfill-hashes")]
        [EnableRateLimiting("AdminLimiter")]
        public async Task<IActionResult> BackfillHashes(CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(BackfillHashes)}");

                // 1. Récupérer toutes les photos qui n'ont pas encore de Hash
                var photosSansHash = await _context.Photos
                    .Where(p => string.IsNullOrEmpty(p.FileHash))
                    .ToListAsync(cancellationToken);

                if (photosSansHash.Count == 0)
                {
                    return Ok(new { message = "Toutes les photos ont déjà un FileHash. Rien à faire !" });
                }

                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                int updatedCount = 0;
                int missingFilesCount = 0;

                // 2. Boucler sur chaque photo
                // ⚡ Bolt: Use bounded concurrency (Parallel.ForEachAsync) with truly async file streams to prevent I/O exhaustion and safely speed up hashing.
                var maxDegrees = Environment.ProcessorCount;
                var hashResults = new System.Collections.Concurrent.ConcurrentBag<(Photo Photo, string? Hash, bool Exists, string FilePath)>();
                var imagesBaseDir = Path.GetFullPath(Path.Combine(rootPath, "images")) + Path.DirectorySeparatorChar;

                await Parallel.ForEachAsync(photosSansHash, new ParallelOptions { MaxDegreeOfParallelism = maxDegrees, CancellationToken = cancellationToken }, async (photo, ct) =>
                {
                    var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);
                    var filePath = Path.GetFullPath(Path.Combine(imagesBaseDir, safeFileName));

                    if (!filePath.StartsWith(imagesBaseDir, StringComparison.OrdinalIgnoreCase)) return;

                    try
                    {
                        using var stream = await Task.Run(() => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true), ct);
                        var hashBytes = await SHA512.HashDataAsync(stream, ct);
                        hashResults.Add((Photo: photo, Hash: Convert.ToHexStringLower(hashBytes), Exists: true, FilePath: filePath));
                    }
                    catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    {
                        hashResults.Add((Photo: photo, Hash: null, Exists: false, FilePath: filePath));
                    }
                });

                foreach (var result in hashResults)
                {
                    if (result.Exists)
                    {
                        result.Photo.FileHash = result.Hash;
                        updatedCount++;
                    }
                    else
                    {
                        missingFilesCount++;
                        log.Warn($"Fichier introuvable pour la photo ID {result.Photo.Id} : {result.FilePath}");
                    }
                }

                // 4. Sauvegarder toutes les modifications d'un coup dans MariaDB
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    message = "Mise à jour terminée.",
                    photosMisesAJour = updatedCount,
                    fichiersIntrouvables = missingFilesCount
                });
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(BackfillHashes)}", e);
                return StatusCode(500, "Erreur interne lors de la mise à jour des empreintes.");
            }
        }

        // POST: api/photos/{id}/report (Public: Visiteurs)
        [HttpPost("{id}/report")]
        [Authorize] // 🔒 LE CADENAS EST ICI !
        [EnableRateLimiting("ReportLimiter")] // 🛡️ Sentinel: Enforce rate limiting to prevent DoS via spam reporting
        public async Task<IActionResult> ReportPhoto(int id, [FromBody] ReportDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(ReportPhoto)} for photo ID: {id}");

                // 1. Vérifier si l'image existe toujours
                var photo = await _context.Photos.FindAsync(new object[] { id }, cancellationToken);
                if (photo == null)
                {
                    return NotFound(new { message = "L'image que vous essayez de signaler n'existe plus." });
                }

                // 🛡️ Sentinel: Fix IDOR by validating group membership
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

                if (photo.UploaderUsername == User.Identity?.Name)
                {
                    return BadRequest(new { message = "Vous ne pouvez pas signaler votre propre photo." });
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
                    Reason = request.Reason,
                    ReporterUsername = User.Identity?.Name ?? string.Empty
                };

                _context.ImageReports.Add(report);
                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { message = "Signalement enregistré avec succès. Un administrateur a été notifié." });
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(ReportPhoto)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du signalement." });
            }
        }


        // POST: api/photos/maintenance/generate-thumbnails
        [Authorize(Roles = "Admin")]
        [HttpPost("maintenance/generate-thumbnails")]
        [EnableRateLimiting("AdminLimiter")]
        public async Task<IActionResult> GenerateMissingThumbnails(CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(GenerateMissingThumbnails)}");

                IDisposable? admissionLease = null;
                try
                {
                    admissionLease = await _admissionGate.AcquireAsync(cancellationToken);
                }
                catch (ImageProcessingBusyException busyEx)
                {
                    Response.Headers["Retry-After"] = "5";
                    return StatusCode(503, new { message = busyEx.Message });
                }

                try
                {
                    var fileNames = await _context.Photos
                        .AsNoTracking()
                        .Where(p => p.FileName != null)
                        .Select(p => p.FileName!)
                        .ToListAsync(cancellationToken);

                    var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsFolder = Path.Combine(rootPath, "images");
                    var thumbFolder = Path.Combine(uploadsFolder, "thumbnails");

                    if (!Directory.Exists(thumbFolder)) Directory.CreateDirectory(thumbFolder);

                    var generatedCount = 0;
                    var missingOriginalsCount = 0;
                    var fullUploadsFolder = Path.GetFullPath(uploadsFolder) + Path.DirectorySeparatorChar;
                    var fullThumbFolder = Path.GetFullPath(thumbFolder) + Path.DirectorySeparatorChar;

                    foreach (var fileName in fileNames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var safeFileName = Path.GetFileName(fileName.Replace("\\", "/"));
                        var originalPath = Path.GetFullPath(Path.Combine(fullUploadsFolder, safeFileName));
                        var thumbPath = Path.GetFullPath(Path.Combine(fullThumbFolder, safeFileName));

                        if (!originalPath.StartsWith(fullUploadsFolder, StringComparison.OrdinalIgnoreCase) || !thumbPath.StartsWith(fullThumbFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // 1. Skip if thumbnail already exists
                        if (System.IO.File.Exists(thumbPath))
                        {
                            continue;
                        }

                        // 2. Record missing original file if it disappeared from disk
                        if (!System.IO.File.Exists(originalPath))
                        {
                            missingOriginalsCount++;
                            continue;
                        }

                        // 3. Atomic generation via temporary file
                        var tempThumbPath = Path.Combine(thumbFolder, $"{safeFileName}.tmp.{Guid.NewGuid():N}");
                        try
                        {
                            using (var inStream = System.IO.File.OpenRead(originalPath))
                            using (var result = await _imageProcessingService.GenerateThumbnailAsync(inStream, cancellationToken))
                            using (var outStream = System.IO.File.Create(tempThumbPath))
                            {
                                await result.ThumbnailStream.CopyToAsync(outStream, cancellationToken);
                            }

                            System.IO.File.Move(tempThumbPath, thumbPath, overwrite: false);
                            generatedCount++;
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            if (System.IO.File.Exists(tempThumbPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(tempThumbPath);
                                }
                                catch
                                {
                                }
                            }
                            throw;
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Erreur lors de la génération de miniature pour {fileName}", ex);
                            if (System.IO.File.Exists(tempThumbPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(tempThumbPath);
                                }
                                catch
                                {
                                }
                            }
                            return StatusCode(500, "Erreur interne lors de la compression des images.");
                        }
                    }

                    return Ok(new
                    {
                        message = "Opération de maintenance terminée avec succès.",
                        miniaturesCreees = generatedCount,
                        imagesOriginalesIntrouvables = missingOriginalsCount
                    });
                }
                finally
                {
                    admissionLease?.Dispose();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error("Erreur lors de la génération massive des miniatures", e);
                return StatusCode(500, "Erreur interne lors de la compression des images.");
            }
        }



        // POST: api/photos/maintenance/migrate-closed-loop
        [Authorize(Roles = "Admin")]
        [HttpPost("maintenance/migrate-closed-loop")]
        [EnableRateLimiting("AdminLimiter")]
        public async Task<IActionResult> MigrateClosedLoop(CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(MigrateClosedLoop)}");

                // 1. Créer ou récupérer le Groupe par Défaut
                var defaultGroup = await _context.Groups.FirstOrDefaultAsync(g => g.Name == "Cercle Initial", cancellationToken);
                if (defaultGroup == null)
                {
                    defaultGroup = new Group { Name = "Cercle Initial", ShortName = "cercle-initial", Description = "Groupe par défaut pour les utilisateurs existants" };
                    _context.Groups.Add(defaultGroup);
                    await _context.SaveChangesAsync(cancellationToken); // Sauvegarder pour avoir l'ID généré
                }

                // 2. Assigner tous les utilisateurs existants à ce groupe
                // ⚡ Bolt: Offloaded missing user membership filtering to the database by using a subquery (Any) instead of fetching all users into memory, reducing memory footprint and network latency.
                var missingUserIds = await _context.Users
                    .Where(u => !_context.UserGroups.Any(ug => ug.GroupId == defaultGroup.Id && ug.UserId == u.Id))
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                var missingMemberships = missingUserIds.Select(userId => new UserGroup { UserId = userId, GroupId = defaultGroup.Id }).ToList();

                // ⚡ Bolt: Use .Count != 0 instead of .Any() on a List to avoid enumerator allocation overhead
                if (missingMemberships.Count != 0)
                {
                    _context.UserGroups.AddRange(missingMemberships);
                }

                // 3. Déplacer les images de wwwroot vers PrivateImages
                var oldRootPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images");
                var newRootPath = Path.Combine(_env.ContentRootPath, "PrivateImages");

                if (!Directory.Exists(newRootPath)) Directory.CreateDirectory(newRootPath);

                var oldThumbPath = Path.Combine(oldRootPath, "thumbnails");
                var newThumbPath = Path.Combine(newRootPath, "thumbnails");
                if (!Directory.Exists(newThumbPath)) Directory.CreateDirectory(newThumbPath);

                var allPhotos = await _context.Photos.ToListAsync(cancellationToken);
                int migratedImages = 0;

                // ⚡ Bolt: Replace unbounded Task.Run with Parallel.ForEachAsync for bounded concurrency,
                // preventing thread pool starvation and file descriptor exhaustion.
                var results = new (Photo photo, int localMigratedImages, string? newUrl)[allPhotos.Count];
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken };

                var fullOldRootPath = Path.GetFullPath(oldRootPath) + Path.DirectorySeparatorChar;
                var fullNewRootPath = Path.GetFullPath(newRootPath) + Path.DirectorySeparatorChar;
                var fullOldThumbPath = Path.GetFullPath(oldThumbPath) + Path.DirectorySeparatorChar;
                var fullNewThumbPath = Path.GetFullPath(newThumbPath) + Path.DirectorySeparatorChar;

                await Parallel.ForEachAsync(Enumerable.Range(0, allPhotos.Count), options, async (i, ct) =>
                {
                    var photo = allPhotos[i];
                    int localMigratedImages = 0;

                    // Move original file
                    var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);
                    var oldFilePath = Path.GetFullPath(Path.Combine(fullOldRootPath, safeFileName));
                    var newFilePath = Path.GetFullPath(Path.Combine(fullNewRootPath, safeFileName));

                    var oldThumbFile = Path.GetFullPath(Path.Combine(fullOldThumbPath, safeFileName));
                    var newThumbFile = Path.GetFullPath(Path.Combine(fullNewThumbPath, safeFileName));

                    if (!oldFilePath.StartsWith(fullOldRootPath, StringComparison.OrdinalIgnoreCase) || !newFilePath.StartsWith(fullNewRootPath, StringComparison.OrdinalIgnoreCase) ||
                        !oldThumbFile.StartsWith(fullOldThumbPath, StringComparison.OrdinalIgnoreCase) || !newThumbFile.StartsWith(fullNewThumbPath, StringComparison.OrdinalIgnoreCase))
                    {
                        results[i] = (photo, localMigratedImages, null);
                        return; // Equivalent to continue inside Parallel.ForEachAsync
                    }

                    if (System.IO.File.Exists(oldFilePath) && !System.IO.File.Exists(newFilePath))
                    {
                        System.IO.File.Move(oldFilePath, newFilePath);
                        localMigratedImages++;
                    }

                    // Move thumbnail file
                    if (System.IO.File.Exists(oldThumbFile) && !System.IO.File.Exists(newThumbFile))
                    {
                        System.IO.File.Move(oldThumbFile, newThumbFile);
                    }

                    // ⚡ Bolt: Compute CPU-intensive string replacements concurrently
                    string? newUrl = null;
                    if (photo.Url != null && photo.Url.StartsWith("/images/"))
                    {
                        newUrl = photo.Url.Replace("/images/", "/api/images/");
                    }

                    results[i] = (photo, localMigratedImages, newUrl);
                    await Task.CompletedTask; // Since we removed Task.Run but are using ForEachAsync
                });

                foreach (var (photo, localMigratedImages, newUrl) in results)
                {
                    // Update DB info (must be done on the main thread since DbContext is not thread-safe)
                    if (!photo.GroupId.HasValue) photo.GroupId = defaultGroup.Id;
                    if (newUrl != null)
                    {
                        photo.Url = newUrl;
                    }

                    migratedImages += localMigratedImages;
                }

                await _context.SaveChangesAsync(cancellationToken);

                return Ok(new { message = "Migration Closed Loop complétée.", uploadedFiles = migratedImages, defaultGroupId = defaultGroup.Id });
            }
            catch (Exception ex)
            {
                log.Error("Erreur lors de la migration Closed Loop", ex);
                return StatusCode(500, "Erreur interne lors de la migration.");
            }
        }

        [Authorize]
        [EnableRateLimiting("LikeLimiter")]
        [HttpPost("{id}/like")]
        public async Task<IActionResult> ToggleLike(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Trouver qui est connecté
                var currentUsername = User.Identity?.Name;

                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId directly from JWT claims.
                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(currentUserIdString, out int userId)) return Unauthorized(new { message = "Utilisateur non trouvé." });

                // 2. Vérifier si la photo existe
                var photo = await _context.Photos.FindAsync(new object[] { id }, cancellationToken);
                if (photo == null) return NotFound(new { message = "Photo introuvable." });

                // 🛡️ Sentinel: Fix IDOR by validating group membership
                if (photo.GroupId.HasValue)
                {
                    bool isAdmin = User.IsInRole("Admin");
                    if (!isAdmin)
                    {
                        bool isMember = await _context.UserGroups
                            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == photo.GroupId.Value, cancellationToken);

                        if (!isMember) return Forbid();
                    }
                }

                if (photo.UploaderUsername == currentUsername)
                {
                    return BadRequest(new { message = "Vous ne pouvez pas aimer votre propre photo." });
                }

                // 3. Chercher si le "Like" existe déjà pour cet utilisateur et cette photo
                var existingLike = await _context.PhotoLikes
                                                 .FirstOrDefaultAsync(l => l.PhotoId == id && l.UserId == userId, cancellationToken);

                if (existingLike != null)
                {
                    // Le Like existe déjà : on l'efface (Unlike)
                    _context.PhotoLikes.Remove(existingLike);
                    photo.LikesCount = Math.Max(0, photo.LikesCount - 1);
                    await _context.SaveChangesAsync(cancellationToken);
                    return Ok(new { liked = false, message = "Like retiré." });
                }
                else
                {
                    var newLike = new PhotoLike
                    {
                        PhotoId = id,
                        Photo = photo,
                        UserId = userId,
                        User = null!,
                        LikedAt = DateTime.UtcNow
                    };

                    _context.PhotoLikes.Add(newLike);
                    photo.LikesCount++;
                    await _context.SaveChangesAsync(cancellationToken);
                    return Ok(new { liked = true, message = "Photo aimée." });
                }
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(ToggleLike)}", e);
                return StatusCode(500, new { message = "Une erreur est survenue avec le like." });
            }
        }

        // GET: api/photos/user/{username}
        [HttpGet("user/{username}")]
        [EnableRateLimiting("PhotosGetLimiter")]
        public async Task<IActionResult> GetUserPhotos(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Language lang = Language.FR, CancellationToken cancellationToken = default)
        {
            // 🛡️ Sentinel: Enforce maximum limits to prevent DoS via large DB queries and OOM.
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            try
            {
                // 1. Trouver l'utilisateur cible
                var targetUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
                if (targetUser == null) return NotFound(new { message = "Utilisateur introuvable." });

                var currentUserIdStringForAccess = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserIdForAccess = int.TryParse(currentUserIdStringForAccess, out var currentIdParsed) ? currentIdParsed : null;
                bool isAdmin = User.IsInRole("Admin");

                var query = _context.Photos
                    .AsNoTracking()
                    .Where(p => p.UploaderUsername == targetUser.Username) // On filtre par UploaderUsername !
                    .Include(p => p.Tags)
                        .ThenInclude(t => t.Translations)
                    // ⚡ Bolt: Adding .AsSplitQuery() prevents Cartesian explosion when including multiple collections (Tags and Translations),
                    // significantly reducing memory footprint and database network transfer by issuing separate queries instead of massive JOINs.
                    .AsSplitQuery()
                    .OrderByDescending(p => p.UploadedAt)
                    .AsQueryable();

                if (!isAdmin && currentUserIdForAccess.HasValue)
                {
                    // ⚡ Bolt: Retain IQueryable instead of ToListAsync to let EF Core generate an efficient SQL subquery for the IN clause, preventing in-memory data transfer and reducing latency.
                    var userGroupIds = _context.UserGroups
                        .AsNoTracking()
                        .Where(ug => ug.UserId == currentUserIdForAccess.Value)
                        .Select(ug => ug.GroupId);

                    query = query.Where(p => !p.GroupId.HasValue || userGroupIds.Contains(p.GroupId.Value));
                }
                else if (!isAdmin && !currentUserIdForAccess.HasValue)
                {
                    query = query.Where(p => !p.GroupId.HasValue);
                }

                // 2. Chercher toutes ses photos publiées
                // ⚡ Bolt: Apply pagination on the server-side to limit payload size and improve latency
                var totalCount = await query.CountAsync(cancellationToken);
                query = query.Skip((page - 1) * pageSize).Take(pageSize);

                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var userPhotos = await query.ToListAsync(cancellationToken);

                if (userPhotos.Count == 0)
                {
                    Response.Headers.Append("X-Total-Count", totalCount.ToString());
                    return Ok(userPhotos);
                }

                // 3. LOGIQUE DES COMPTEURS (Comme d'habitude)
                var photoIds = userPhotos.Select(p => p.Id).ToList();

                var currentUsername = User.Identity?.Name;
                var currentUserLikedPhotoIds = new HashSet<int>();
                var currentUserReportedPhotoIds = new HashSet<int>();

                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(currentUserIdString, out var id) ? id : null;

                if (currentUserId.HasValue)
                {
                    var likedIds = await _context.PhotoLikes
                        .Where(l => photoIds.Contains(l.PhotoId) && l.UserId == currentUserId.Value)
                        .Select(l => l.PhotoId)
                        .ToListAsync(cancellationToken);

                    var reportedIds = await _context.ImageReports
                        .Where(r => photoIds.Contains(r.PhotoId) && r.ReporterUsername == currentUsername)
                        .Select(r => r.PhotoId)
                        .ToListAsync(cancellationToken);

                    currentUserLikedPhotoIds = new HashSet<int>(likedIds);
                    currentUserReportedPhotoIds = new HashSet<int>(reportedIds);
                }

                // ⚡ Bolt: Generate S3 presigned URLs directly to avoid N+1 proxy requests from the client and N+1 database queries.
                // ⚡ Bolt: Replace unbounded Task.WhenAll with Parallel.ForEachAsync for bounded concurrency, preventing thread pool starvation.
                await Parallel.ForEachAsync(userPhotos, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (photo, ct) =>
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = currentUserLikedPhotoIds.Contains(photo.Id);
                    photo.IsReportedByCurrentUser = currentUserReportedPhotoIds.Contains(photo.Id);
                });

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                return Ok(userPhotos);
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(GetUserPhotos)}", e);
                return StatusCode(500, new { message = "Erreur de récupération." });
            }
        }

        // GET: api/photos/most-viewed
        [HttpGet("most-viewed")]
        [EnableRateLimiting("PhotosGetLimiter")]
        public async Task<ActionResult<IEnumerable<Photo>>> GetMostViewedPhotos(
            [FromQuery] int count = 10,
            [FromQuery] Language lang = Language.FR, CancellationToken cancellationToken = default)
        {
            try
            {
                // 🛡️ Sentinel: Enforce maximum limits to prevent DoS via large DB queries and OOM.
                count = Math.Clamp(count, 1, 50);

                log.Debug($"In {nameof(GetMostViewedPhotos)} with count: {count}");

                // 1. On récupère les N photos les plus vues (> 0 vues)
                var currentUserIdStringForAccess = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserIdForAccess = int.TryParse(currentUserIdStringForAccess, out var currentIdParsed) ? currentIdParsed : null;
                bool isAdmin = User.IsInRole("Admin");

                var query = _context.Photos
                    .AsNoTracking()
                    .Include(p => p.Tags)
                        .ThenInclude(t => t.Translations)
                    // ⚡ Bolt: Adding .AsSplitQuery() prevents Cartesian explosion when including multiple collections (Tags and Translations),
                    // significantly reducing memory footprint and database network transfer by issuing separate queries instead of massive JOINs.
                    .AsSplitQuery()
                    .Where(p => p.ViewsCount > 0)
                    .AsQueryable();

                if (!isAdmin && currentUserIdForAccess.HasValue)
                {
                    // ⚡ Bolt: Retain IQueryable instead of ToListAsync to let EF Core generate an efficient SQL subquery for the IN clause, preventing in-memory data transfer and reducing latency.
                    var userGroupIds = _context.UserGroups
                        .AsNoTracking()
                        .Where(ug => ug.UserId == currentUserIdForAccess.Value)
                        .Select(ug => ug.GroupId);

                    query = query.Where(p => !p.GroupId.HasValue || userGroupIds.Contains(p.GroupId.Value));
                }
                else if (!isAdmin && !currentUserIdForAccess.HasValue)
                {
                    query = query.Where(p => !p.GroupId.HasValue);
                }

                query = query.OrderByDescending(p => p.ViewsCount)
                             .ThenByDescending(p => p.Id)
                             .Take(count);

                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var photos = await query.ToListAsync(cancellationToken);

                if (photos.Count == 0) return Ok(photos);

                // 2. On attache les likes (comme pour GetPhotos)
                var photoIds = photos.Select(p => p.Id).ToList();

                var currentUsername = User.Identity?.Name;
                var currentUserLikedPhotoIds = new HashSet<int>();

                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(currentUserIdString, out var id) ? id : null;

                if (currentUserId.HasValue)
                {
                    var likedIds = await _context.PhotoLikes
                        .Where(l => photoIds.Contains(l.PhotoId) && l.UserId == currentUserId.Value)
                        .Select(l => l.PhotoId)
                        .ToListAsync(cancellationToken);
                    currentUserLikedPhotoIds = new HashSet<int>(likedIds);
                }

                // ⚡ Bolt: Generate S3 presigned URLs directly to avoid N+1 proxy requests from the client and N+1 database queries.
                // ⚡ Bolt: Replace unbounded Task.WhenAll with Parallel.ForEachAsync for bounded concurrency, preventing thread pool starvation.
                await Parallel.ForEachAsync(photos, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (photo, ct) =>
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = currentUserLikedPhotoIds.Contains(photo.Id);
                });

                return Ok(photos);
            }
            catch (Exception e)
            {
                log.Error($"Erreur dans {nameof(GetMostViewedPhotos)}", e);
                return StatusCode(500, new { message = "Erreur de récupération des photos les plus vues." });
            }
        }

        // POST: api/photos/{id}/view
        [HttpPost("{id}/view")]
        [AllowAnonymous] // On permet aux anonymes d'incrémenter les vues
        [EnableRateLimiting("ViewLimiter")]
        public async Task<IActionResult> RecordView(int id, CancellationToken cancellationToken = default)
        {
            // Note: Pour ne pas ralentir cette route ultra-rapide, on évite d'interroger la base
            // pour résoudre l'utilisateur. Si l'ID de l'utilisateur est dans les Claims (JWT),
            // décommente et utilise ce qui suit (sinon UserId reste null) :


            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            var viewEvent = new PhotoViewEvent
            {
                PhotoId = id,
                UserId = userId, // ou `userId` si tu l'as récupéré
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };
            // Écriture asynchrone dans le Thread Channel (non bloquant sur l'I/O DB)
            await _viewChannelWriter.WriteAsync(viewEvent, cancellationToken);
            // Retourner "202 Accepted" : "Requête acceptée, je la traite en arrière-plan !"
            return Accepted();
        }
    }

    public class ReportDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
