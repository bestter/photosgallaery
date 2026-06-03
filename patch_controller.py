import re

with open('PhotoAppApi/Controllers/PhotosController.cs', 'r') as f:
    content = f.read()

# Modify UploadPhotos signature back to having IModerationService
content = re.sub(
    r'public async Task<IActionResult> UploadPhotos\(\[FromForm\] IList<IFormFile> files, \[FromForm\] string tags, \[FromForm\] Guid\? groupId, \[FromForm\] bool includeGps = true, CancellationToken cancellationToken = default\)',
    r'public async Task<IActionResult> UploadPhotos([FromForm] IList<IFormFile> files, [FromServices] IModerationService? moderationService, [FromForm] string tags, [FromForm] Guid? groupId, [FromForm] bool includeGps = true, CancellationToken cancellationToken = default)',
    content
)

# Revert the IModerationService logic
content = re.sub(
    r'if \(_moderationService == null\)\n                \{\n                    log.Error\("ModerationService is not configured. Failing closed to prevent unmoderated uploads."\);\n                    return StatusCode\(500, new \{ message = "Le service de modération est indisponible. Le téléversement est bloqué." \}\);\n                \}',
    r'var theModerationService = moderationService ?? _moderationService;\n                if (theModerationService == null)\n                {\n                    log.Error("ModerationService is not configured. Failing closed to prevent unmoderated uploads.");\n                    return StatusCode(500, new { message = "Le service de modération est indisponible. Le téléversement est bloqué." });\n                }\n                var moderationSvc = theModerationService;',
    content
)

content = re.sub(
    r'await _moderationService\.CheckImageAsync',
    r'await moderationSvc.CheckImageAsync',
    content
)

with open('PhotoAppApi/Controllers/PhotosController.cs', 'w') as f:
    f.write(content)
