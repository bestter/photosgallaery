using log4net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;

namespace PhotoAppApi.Services;

public class HashCalculationBackgroundService : BackgroundService
{
        private static readonly ILog log = LogManager.GetLogger(typeof(HashCalculationBackgroundService));

    private readonly IServiceProvider _serviceProvider;
    public HashCalculationBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.Info("HashCalculationBackgroundService is starting.");

        try
        {
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
                        log.Info("Found {photosWithoutHash.Count} photos without hash. Processing...");

                        var rootContentPath = env.ContentRootPath;
                        var privateImagesFolder = Path.Combine(rootContentPath, "PrivateImages");

                        var rootWebPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        var publicImagesFolder = Path.Combine(rootWebPath, "images");

                        using var sha512 = SHA512.Create();

                        foreach (var photo in photosWithoutHash)
                        {
                            var safeFileName = Path.GetFileName(photo.FileName.Replace("\\", "/"));
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
                                log.Warn($"File not found for photo {photo.Id}: {photo.FileName}");
                                photo.FileHash = "FILE_MISSING";
                            }
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                        log.Info("Processed {photosWithoutHash.Count} photos.");
                    }
                    else
                    {
                        // No photos without hash, wait longer before checking again
                        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // This is expected during shutdown, bubble it up to the outer catch
                    throw;
                }
                catch (Exception ex)
                {
                    log.Error("Error occurred executing HashCalculationBackgroundService.", ex);
                }

                // Small delay to prevent tight loop if there are many photos to process or an error occurs
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, exit gracefully
            log.Info("HashCalculationBackgroundService is stopping cleanly.");
        }
    }
}
