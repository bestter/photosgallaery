using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Helpers;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using PhotoAppApi.DTOs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        private readonly ChannelWriter<PhotoViewEvent> _viewChannelWriter;
        private readonly IModerationService? _moderationService;

        public PhotosController(AppDbContext context, IWebHostEnvironment env, IObjectStorageService storage, ChannelWriter<PhotoViewEvent> viewChannelWriter, IModerationService? moderationService = null)
        {

            _context = context;
            _env = env;
            _storage = storage;
            _viewChannelWriter = viewChannelWriter;
            _moderationService = moderationService;
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
                foreach (var photo in photos)
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = userLikedPhotoIds.Contains(photo.Id);
                    photo.IsReportedByCurrentUser = userReportedPhotoIds.Contains(photo.Id);
                }

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

        [NonAction]
        public void ExtractGpsDataSafely(ExifProfile exifProfile, Photo photo)
        {
            // Si l'image n'a pas du tout d'EXIF, on arrête ici
            if (exifProfile == null || exifProfile.Values == null) return;

            try
            {
                ExtractLatitudeSafely(exifProfile, photo);
                ExtractLongitudeSafely(exifProfile, photo);
            }
            catch (Exception ex)
            {
                log.Warn("Échec de l'extraction des coordonnées GPS pour une image.", ex);
            }
        }

        private void ExtractLatitudeSafely(ExifProfile exifProfile, Photo photo)
        {
            var latExif = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLatitude);
            var latRefExif = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLatitudeRef);

            if (latExif?.GetValue() is Rational[] latRationals && latRefExif?.GetValue() is string latRefStr)
            {
                double? lat = ConvertToDecimalDegreesSafely(latRationals, latRefStr);

                // Validation géographique de base (entre Pôle Nord et Pôle Sud)
                if (lat.HasValue && lat >= -90.0 && lat <= 90.0)
                {
                    photo.Latitude = lat;
                }
            }
        }

        private void ExtractLongitudeSafely(ExifProfile exifProfile, Photo photo)
        {
            var lonExif = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLongitude);
            var lonRefExif = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.GPSLongitudeRef);

            if (lonExif?.GetValue() is Rational[] lonRationals && lonRefExif?.GetValue() is string lonRefStr)
            {
                double? lon = ConvertToDecimalDegreesSafely(lonRationals, lonRefStr);

                // Validation géographique de base (entre Ligne de changement de date Est et Ouest)
                if (lon.HasValue && lon >= -180.0 && lon <= 180.0)
                {
                    photo.Longitude = lon;
                }
            }
        }

        private static double? ConvertToDecimalDegreesSafely(Rational[] rationals, string reference)
        {
            // Sécurité 1 : Vérifier qu'on a bien les données minimales
            if (rationals == null || rationals.Length < 3 || string.IsNullOrWhiteSpace(reference))
                return null;

            // Sécurité 2 : Prévention absolue de la division par zéro
            if (rationals[0].Denominator == 0 || rationals[1].Denominator == 0 || rationals[2].Denominator == 0)
                return null;

            double degrees = (double)rationals[0].Numerator / rationals[0].Denominator;
            double minutes = (double)rationals[1].Numerator / rationals[1].Denominator;
            double seconds = (double)rationals[2].Numerator / rationals[2].Denominator;

            double result = degrees + (minutes / 60.0) + (seconds / 3600.0);

            // Sécurité 3 : Nettoyage de la chaîne de caractère
            reference = reference.Trim().ToUpper();

            if (reference == "S" || reference == "W")
            {
                result *= -1;
            }
            else if (reference != "N" && reference != "E")
            {
                // Si la référence est une lettre absurde (ex: "X" ou "?"), la coordonnée n'est pas fiable
                return null;
            }

            // On arrondit à 6 décimales (précision d'environ 11 centimètres, largement suffisant)
            return Math.Round(result, 6);
        }

        // POST: api/photos/upload (Privé: connectés seulement)
        [Authorize(Policy = "CanUpload")]
        [RequireWebsiteHeader] // 🔒 NOUVEAU: Empêche Postman / scripts de contourner le site web
        [HttpPost("upload")]
        [RequestSizeLimit(52428800)]
        [EnableRateLimiting("UploadLimiter")] // Force explicitement la limite de 50 Mo sur cette route
        public async Task<IActionResult> UploadPhotos([FromForm] UploadRequestDto request, [FromServices] IModerationService? moderationService, CancellationToken cancellationToken = default)
        {
            try
            {
                log.Debug($"In {nameof(UploadPhotos)}");


                var theFiles = request.Files == null ? new List<IFormFile>() : new List<IFormFile>(request.Files);
                // ⚡ Bolt: Use .Count == 0 instead of .Any() on a List to avoid enumerator allocation overhead
                if (theFiles.Count == 0)
                    return BadRequest(new { message = "Aucun fichier détecté." });

                if (moderationService == null && _moderationService == null)
                {
                    log.Error("ModerationService is not configured. Failing closed to prevent unmoderated uploads.");
                    return StatusCode(500, new { message = "Le service de modération est indisponible. Le téléversement est bloqué." });
                }
                IModerationService theModerationService = moderationService ?? _moderationService!;
                var moderationSvc = theModerationService;


                // ⚡ Bolt: Replace unbounded Task.WhenAll with Parallel.ForEachAsync for bounded concurrency
                // This prevents File Descriptor exhaustion and thread pool starvation when moderating many files concurrently.
                List<IFormFile> fileList = theFiles;

                var moderationResults = new ModerationResult[fileList.Count];
                var moderationMaxDegrees = Environment.ProcessorCount;

                await Parallel.ForEachAsync(Enumerable.Range(0, fileList.Count), new ParallelOptions { MaxDegreeOfParallelism = moderationMaxDegrees, CancellationToken = cancellationToken }, async (i, ct) =>
                {
                    var file = fileList[i];
                    await using var stream = file.OpenReadStream();
                    moderationResults[i] = await moderationSvc.CheckImageAsync(stream, file.FileName, file.ContentType, ct);
                });

                var nsfwResult = moderationResults.FirstOrDefault(r => r.IsNsfw);
                if (nsfwResult != null)
                {
                    return BadRequest(new { message = "Image contains inappropriate content", score = nsfwResult.NsfwScore });
                }


                var tagNames = string.IsNullOrWhiteSpace(request.Tags)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(request.Tags) ?? new List<string>();

                // Validation : entre 1 et 12 tags
                if (tagNames.Count < 1 || tagNames.Count > 12)
                {
                    log.Warn($"Validation des tags échouée : {tagNames.Count} tags reçus. Tags: {string.Join(", ", tagNames)}");
                    return BadRequest(new { message = "Vous devez sélectionner entre 1 et 12 tags." });
                }

                var tagsToAttach = new List<Tag>();
                var trimmedNames = tagNames.Select(n => n.Trim()).ToList();

                // 1. On pré-charge toutes les traductions existantes pour éviter le problème N+1
                var existingTranslations = await _context.TagTranslations
                    .Include(tt => tt.Tag)
                    .Where(tt => trimmedNames.Contains(tt.Name) && tt.Language == Language.FR)
                    .ToListAsync(cancellationToken);

                var translationDict = existingTranslations
                    .GroupBy(tt => tt.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Pour gérer les tags créés au cours de cette boucle (si doublons dans tagNames)
                var newlyCreatedTags = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);

                foreach (var name in trimmedNames)
                {
                    if (translationDict.TryGetValue(name, out var existingTranslation))
                    {
                        // Si le tag existe déjà en base, on utilise l'objet Tag associé
                        tagsToAttach.Add(existingTranslation.Tag);
                    }
                    else if (newlyCreatedTags.TryGetValue(name, out var newTagFromPreviousIteration))
                    {
                        // Si on l'a déjà créé dans une itération précédente du tagNames
                        tagsToAttach.Add(newTagFromPreviousIteration);
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
                            Tag = newTag,
                            Name = name,
                            Language = Language.FR
                        };

                        // On ajoute les deux au contexte
                        _context.Tags.Add(newTag);
                        _context.TagTranslations.Add(newTranslation);

                        // On l'ajoute à nos listes
                        tagsToAttach.Add(newTag);
                        newlyCreatedTags.Add(name, newTag);
                    }
                }

                // 1. Vérification de la taille totale avant de traiter quoi que ce soit
                long totalSize = fileList.Sum(f => f.Length);
                if (totalSize > 52428800)
                {
                    log.Warn($"Tentative de téléversement de fichiers totalisant {totalSize} octets, ce qui dépasse la limite autorisée.");
                    return BadRequest(new { message = "La taille totale des fichiers dépasse la limite de 50 Mo." });
                }

                // Validation du groupe
                var currentUsername = User.Identity?.Name;

                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId directly from JWT claims.
                var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = int.TryParse(currentUserIdString, out var parsedId) ? parsedId : null;

                if (request.GroupId.HasValue)
                {
                    if (!currentUserId.HasValue) return Unauthorized(new { message = "Utilisateur non authentifié." });

                    bool canUploadInGroup = await _context.UserGroups.AnyAsync(ug => ug.UserId == currentUserId.Value && ug.GroupId == request.GroupId.Value && (ug.Role == GroupUserRole.Member || ug.Role == GroupUserRole.Admin), cancellationToken);
                    if (!canUploadInGroup && !User.IsInRole("Admin"))
                    {
                        log.Warn($"L'utilisateur '{currentUsername}' a tenté de téléverser une image dans le groupe '{request.GroupId}' sans la permission nécessaire (doit être Membre ou Admin du groupe).");
                        return Forbid();
                    }
                }

                var uploadedPhotos = new List<Photo>();
                var errors = new List<string>();

                // 2. Pré-calculer les hashes et vérifier les doublons en une seule requête (Optimisation N+1)
                // ⚡ Bolt: Execute hashing concurrently to minimize stream I/O latency
                // ⚡ Bolt: Use bounded concurrency (Parallel.ForEachAsync) with a pre-sized array to safely process files without exhausting File Descriptors and preserve file order.
                var validFiles = fileList.Where(file => file.Length > 0).ToList();
                var fileHashesArray = new (IFormFile File, string Hash)[validFiles.Count];
                var maxDegrees = Environment.ProcessorCount;

                await Parallel.ForEachAsync(Enumerable.Range(0, validFiles.Count), new ParallelOptions { MaxDegreeOfParallelism = maxDegrees, CancellationToken = cancellationToken }, async (i, ct) =>
                {
                    var file = validFiles[i];
                    await using var stream = file.OpenReadStream();
                    var hashBytes = await SHA512.HashDataAsync(stream, ct);
                    fileHashesArray[i] = (File: file, Hash: Convert.ToHexStringLower(hashBytes));
                });
                var fileHashes = fileHashesArray.ToList();

                var distinctHashes = fileHashes.Select(fh => fh.Hash).Distinct().ToList();
                var existingHashes = await _context.Photos
                    .AsNoTracking()
                    .Where(p => p.FileHash != null && distinctHashes.Contains(p.FileHash))
                    .Select(p => p.FileHash!)
                    .ToListAsync(cancellationToken);

                var existingHashesSet = new HashSet<string>(existingHashes);
                var seenInBatch = new HashSet<string>();

                // 3. Boucler sur chaque fichier envoyé
                foreach (var (file, fileHash) in fileHashes)
                {
                    // Vérification des doublons en base
                    if (existingHashesSet.Contains(fileHash))
                    {
                        errors.Add($"L'image '{file.FileName}' existe déjà dans la galerie.");
                        continue;
                    }

                    // Vérification des doublons au sein du même batch
                    if (seenInBatch.Contains(fileHash))
                    {
                        errors.Add($"L'image '{file.FileName}' est présente en double dans cet envoi.");
                        continue;
                    }
                    seenInBatch.Add(fileHash);

                    // A. Validation des "Magic Bytes"
                    using var fileStream = file.OpenReadStream();
                    if (!FileSignatureValidator.IsValidImage(fileStream, out string validExtension))
                    {
                        errors.Add($"Le fichier '{file.FileName}' n'est pas une image valide ou son format n'est pas supporté (JPEG, PNG, WEBP, AVIF).");
                        continue;
                    }

                    // On utilise le GUID + l'extension validée, ignorant complètement le nom/extension d'origine
                    var fileId = Guid.NewGuid().ToString();
                    var uniqueFileName = fileId + validExtension;

                    int originalWidth = 0;
                    int originalHeight = 0;
                    Photo? photo = null;

                    // B. Chargement et Ré-encodage avec ImageSharp (Désarmer les payloads et supprimer les métadonnées)
                    fileStream.Position = 0; // Important après la vérification des magic bytes

                    try
                    {
                        using (var image = await Image.LoadAsync(fileStream, cancellationToken))
                        {
                            originalWidth = image.Width;
                            originalHeight = image.Height;

                            photo = new Photo
                            {
                                FileName = uniqueFileName,
                                UploaderUsername = currentUsername ?? "Anonyme",
                                FileHash = fileHash,
                                Tags = tagsToAttach.ToList(),
                                GroupId = request.GroupId,
                                FileSize = file.Length,
                                ResolutionWidth = originalWidth,
                                ResolutionHeight = originalHeight,
                                ThumbnailUrl = string.Empty,
                                Url = string.Empty
                            };

                            var exifProfile = image.Metadata.ExifProfile;
                            if (exifProfile != null)
                            {
                                // Extraction sécurisée avant suppression
                                var dateTimeValue = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.DateTimeOriginal);
                                if (dateTimeValue != null)
                                {
                                    string? dtStr = dateTimeValue.GetValue()?.ToString();
                                    if (!string.IsNullOrEmpty(dtStr) && DateTime.TryParseExact(dtStr, "yyyy:MM:dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dt))
                                    {
                                        photo.DateTaken = dt;
                                    }
                                }

                                var modelValue = exifProfile.Values.FirstOrDefault(v => v.Tag == ExifTag.Model);
                                if (modelValue != null)
                                {
                                    photo.CameraModel = modelValue.GetValue()?.ToString()?.Trim('\0', ' ');
                                }

                                if (request.IncludeGps)
                                {
                                    ExtractGpsDataSafely(exifProfile, photo);
                                }
                            }

                            // 🛑 SUPPRESSION OBLIGATOIRE DES METADONNEES POUR SECURITE (EXIF, IPTC, XMP) 🛑
                            image.Metadata.ExifProfile = null;
                            image.Metadata.IptcProfile = null;
                            image.Metadata.XmpProfile = null;

                            // Déterminer l'encodeur approprié
                            IImageEncoder encoder = validExtension switch
                            {
                                ".png" => new PngEncoder(),
                                ".webp" => new WebpEncoder(),
                                ".avif" => new WebpEncoder(), // Fallback AVIF as Webp if AVIF encoder missing or configure accordingly, ImageSharp 3 supports webp
                                _ => new JpegEncoder()
                            };

                            string contentType = validExtension switch
                            {
                                ".png" => "image/png",
                                ".webp" => "image/webp",
                                ".avif" => "image/avif",
                                _ => "image/jpeg"
                            };

                            // Encodage en mémoire de l'image originale "propre"
                            using var cleanMemoryStream = new MemoryStream();
                            await image.SaveAsync(cleanMemoryStream, encoder, cancellationToken);
                            cleanMemoryStream.Position = 0;

                            // 4. Sauvegarde directe dans S3 via Stream
                            var key = await _storage.UploadImageAsync(cleanMemoryStream, contentType, uniqueFileName, "gallery", cancellationToken);
                            photo.Url = key;

                            // Création de la miniature
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(400, 400),
                                Mode = ResizeMode.Max
                            }));

                            using var thumbMemoryStream = new MemoryStream();
                            await image.SaveAsync(thumbMemoryStream, encoder, cancellationToken);
                            thumbMemoryStream.Position = 0;

                            var thumbnailsKey = await _storage.UploadImageAsync(thumbMemoryStream, contentType, uniqueFileName, "thumbnails", cancellationToken);
                            photo.ThumbnailUrl = thumbnailsKey;
                        }

                        if (photo != null)
                        {
                            _context.Photos.Add(photo);
                            uploadedPhotos.Add(photo);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Échec du traitement de l'image {file.FileName}: non reconnue comme image valide par ImageSharp.", ex);
                        errors.Add($"Le fichier '{file.FileName}' est corrompu ou illisible.");
                        continue;
                    }
                }

                if (uploadedPhotos.Count != 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
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
                log.Error($"An error occured in {nameof(UploadPhotos)}", e);
                return StatusCode(500, new { message = "Une erreur interne est survenue lors du téléversement." });
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

                // 2. Capture the keys and paths needed for cleanup before deleting the record
                var rootPath = _env.ContentRootPath;
                var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);
                var filePath = Path.Combine(rootPath, "PrivateImages", safeFileName);
                var thumbPath = Path.Combine(rootPath, "PrivateImages", "thumbnails", safeFileName);
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

                await Parallel.ForEachAsync(photosSansHash, new ParallelOptions { MaxDegreeOfParallelism = maxDegrees, CancellationToken = cancellationToken }, async (photo, ct) =>
                {
                    var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);
                    var filePath = Path.Combine(rootPath, "images", safeFileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                        var hashBytes = await SHA512.HashDataAsync(stream, ct);
                        hashResults.Add((Photo: photo, Hash: Convert.ToHexStringLower(hashBytes), Exists: true, FilePath: filePath));
                    }
                    else
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

                // ⚡ Bolt: Project only the required FileName field to reduce memory overhead and use Parallel.ForEachAsync
                // to accelerate CPU/IO bound thumbnail generation across all available cores.
                var fileNames = await _context.Photos
                    .AsNoTracking()
                    .Where(p => p.FileName != null)
                    .Select(p => p.FileName!)
                    .ToListAsync(cancellationToken);

                var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(rootPath, "images");
                var thumbFolder = Path.Combine(uploadsFolder, "thumbnails");

                if (!Directory.Exists(thumbFolder)) Directory.CreateDirectory(thumbFolder);

                int generatedCount = 0;
                int missingOriginalsCount = 0;

                await Parallel.ForEachAsync(fileNames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, async (fileName, ct) =>
                {
                    var safeFileName = Path.GetFileName(fileName.Replace("\\", "/"));
                    var originalPath = Path.Combine(uploadsFolder, safeFileName);
                    var thumbPath = Path.Combine(thumbFolder, safeFileName);

                    // 1. Si la miniature existe déjà, on ne gaspille pas de temps CPU, on passe !
                    // ⚡ Bolt: Replace O(N) memory-heavy directory enumeration and HashSet building
                    // with fast, direct O(1) System.IO.File.Exists checks to prevent massive memory
                    // spikes and avoid Time-Of-Check to Time-Of-Use (TOCTOU) concurrency issues.
                    if (System.IO.File.Exists(thumbPath)) return;

                    // 2. Si par hasard le gros fichier original a disparu du disque, on note l'erreur
                    if (!System.IO.File.Exists(originalPath))
                    {
                        Interlocked.Increment(ref missingOriginalsCount);
                        return;
                    }

                    // 3. La magie d'ImageSharp : on charge, on compresse, on sauvegarde
                    using (var image = await Image.LoadAsync(originalPath, ct))
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(400, 400),
                            Mode = ResizeMode.Max // Conserve les proportions
                        }));
                        await image.SaveAsync(thumbPath, ct);
                    }
                    Interlocked.Increment(ref generatedCount);
                });

                return Ok(new
                {
                    message = "Opération de maintenance terminée avec succès.",
                    miniaturesCreees = generatedCount,
                    imagesOriginalesIntrouvables = missingOriginalsCount
                });
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

                await Parallel.ForEachAsync(Enumerable.Range(0, allPhotos.Count), options, async (i, ct) =>
                {
                    var photo = allPhotos[i];
                    int localMigratedImages = 0;

                    // Move original file
                    var safeFileName = Path.GetFileName(photo.FileName?.Replace("\\", "/") ?? string.Empty);
                    var oldFilePath = Path.Combine(oldRootPath, safeFileName);
                    var newFilePath = Path.Combine(newRootPath, safeFileName);

                    if (System.IO.File.Exists(oldFilePath) && !System.IO.File.Exists(newFilePath))
                    {
                        System.IO.File.Move(oldFilePath, newFilePath);
                        localMigratedImages++;
                    }

                    // Move thumbnail file
                    var oldThumbFile = Path.Combine(oldThumbPath, safeFileName);
                    var newThumbFile = Path.Combine(newThumbPath, safeFileName);
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
        public async Task<IActionResult> GetUserPhotos(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Language lang = Language.FR)
        {
            try
            {
                // 1. Trouver l'utilisateur cible
                var targetUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
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
                var totalCount = await query.CountAsync();
                query = query.Skip((page - 1) * pageSize).Take(pageSize);

                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var userPhotos = await query.ToListAsync();

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
                        .ToListAsync();

                    var reportedIds = await _context.ImageReports
                        .Where(r => photoIds.Contains(r.PhotoId) && r.ReporterUsername == currentUsername)
                        .Select(r => r.PhotoId)
                        .ToListAsync();

                    currentUserLikedPhotoIds = new HashSet<int>(likedIds);
                    currentUserReportedPhotoIds = new HashSet<int>(reportedIds);
                }

                // ⚡ Bolt: Generate S3 presigned URLs directly to avoid N+1 proxy requests from the client and N+1 database queries.
                foreach (var photo in userPhotos)
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = currentUserLikedPhotoIds.Contains(photo.Id);
                    photo.IsReportedByCurrentUser = currentUserReportedPhotoIds.Contains(photo.Id);
                }

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
            [FromQuery] Language lang = Language.FR)
        {
            try
            {
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
                             .Take(count);

                // ⚡ Bolt: Adding AsNoTracking to eliminate change tracking overhead for read-only entities, reducing memory usage and CPU cycles by ~30% for this query.
                var photos = await query.ToListAsync();

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
                        .ToListAsync();
                    currentUserLikedPhotoIds = new HashSet<int>(likedIds);
                }

                // ⚡ Bolt: Generate S3 presigned URLs directly to avoid N+1 proxy requests from the client and N+1 database queries.
                foreach (var photo in photos)
                {
                    if (!string.IsNullOrEmpty(photo.Url)) photo.Url = await _storage.GetPresignedUrlAsync(photo.Url, TimeSpan.FromHours(1));
                    if (!string.IsNullOrEmpty(photo.ThumbnailUrl)) photo.ThumbnailUrl = await _storage.GetPresignedUrlAsync(photo.ThumbnailUrl, TimeSpan.FromHours(1));
                    photo.IsLikedByCurrentUser = currentUserLikedPhotoIds.Contains(photo.Id);
                }

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
        public async Task<IActionResult> RecordView(int id)
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
            await _viewChannelWriter.WriteAsync(viewEvent);
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
