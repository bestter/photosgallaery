using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;

namespace PhotoAppApi.Services;

public class HashCalculationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HashCalculationBackgroundService> _logger;

    public HashCalculationBackgroundService(IServiceProvider serviceProvider, ILogger<HashCalculationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HashCalculationBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                var photosWithoutHash = await dbContext.Photos
                    .Where(p => p.FileHash == null || p.FileHash == string.Empty)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (photosWithoutHash.Count > 0)
                {
                    _logger.LogInformation($"Found {photosWithoutHash.Count} photos without hash. Processing...");

                    var rootContentPath = env.ContentRootPath;
                    var privateImagesFolder = Path.Combine(rootContentPath, "PrivateImages");

                    var rootWebPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var publicImagesFolder = Path.Combine(rootWebPath, "images");

                    using var sha512 = SHA512.Create();

                    foreach (var photo in photosWithoutHash)
                    {
                        var safeFileName = Path.GetFileName(photo.FileName);
                        var privateFilePath = Path.Combine(privateImagesFolder, safeFileName);
                        var publicFilePath = Path.Combine(publicImagesFolder, safeFileName);

                        var filePath = File.Exists(privateFilePath) ? privateFilePath :
                                       File.Exists(publicFilePath) ? publicFilePath : null;

                        if (filePath != null)
                        {
                            using var stream = File.OpenRead(filePath);
                            var hashBytes = await sha512.ComputeHashAsync(stream, stoppingToken);
                            photo.FileHash = Convert.ToHexStringLower(hashBytes);
                        }
                        else
                        {
                            _logger.LogWarning($"File not found for photo {photo.Id}: {photo.FileName}");
                            photo.FileHash = "FILE_MISSING";
                        }
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Processed {photosWithoutHash.Count} photos.");
                }
                else
                {
                    // No photos without hash, wait longer before checking again
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing HashCalculationBackgroundService.");
            }

            // Small delay to prevent tight loop if there are many photos to process or an error occurs
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
