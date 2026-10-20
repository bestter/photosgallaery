using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;

namespace PhotoAppApi.Tests.Services;

public class HashCalculationBackgroundServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _privateImagesDir;
    private readonly string _publicImagesDir;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    public HashCalculationBackgroundServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HashServiceTests_{Guid.NewGuid()}");
        _privateImagesDir = Path.Combine(_tempDir, "PrivateImages");
        _publicImagesDir = Path.Combine(_tempDir, "wwwroot", "images");

        Directory.CreateDirectory(_privateImagesDir);
        Directory.CreateDirectory(_publicImagesDir);

        _dbName = Guid.NewGuid().ToString();

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        envMock.Setup(e => e.WebRootPath).Returns(Path.Combine(_tempDir, "wwwroot"));

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));
        services.AddSingleton(envMock.Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesHashesAndBlocksInvalidOrTraversalFilenames()
    {
        // Arrange
        var privateBytes = Encoding.UTF8.GetBytes("private-image-content");
        var publicBytes = Encoding.UTF8.GetBytes("public-image-content");
        var secretBytes = Encoding.UTF8.GetBytes("top-secret-outside-folder");

        await File.WriteAllBytesAsync(Path.Combine(_privateImagesDir, "private.jpg"), privateBytes, TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_publicImagesDir, "public.jpg"), publicBytes, TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(_tempDir, "secret.jpg"), secretBytes, TestContext.Current.CancellationToken);

        var expectedPrivateHash = Convert.ToHexStringLower(SHA512.HashData(privateBytes));
        var expectedPublicHash = Convert.ToHexStringLower(SHA512.HashData(publicBytes));

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Photos.AddRange(
                new Photo { Id = 1, FileName = "private.jpg" },
                new Photo { Id = 2, FileName = "public.jpg" },
                new Photo { Id = 3, FileName = "missing.jpg" },
                new Photo { Id = 4, FileName = "" },
                new Photo { Id = 5, FileName = ".." },
                new Photo { Id = 6, FileName = "folder/" },
                new Photo { Id = 7, FileName = "bad\0name.jpg" }
            );
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var service = new HashCalculationBackgroundService(_serviceProvider);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Wait briefly for the first iteration to complete before stopping
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        while (!cts.IsCancellationRequested)
        {
            using var checkScope = _serviceProvider.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var remaining = await checkDb.Photos.CountAsync(p => p.FileHash == null, cts.Token);
            if (remaining == 0)
            {
                break;
            }
            await Task.Delay(50, cts.Token);
        }

        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        using var assertScope = _serviceProvider.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var photos = await assertDb.Photos.OrderBy(p => p.Id).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedPrivateHash, photos[0].FileHash);
        Assert.Equal(expectedPublicHash, photos[1].FileHash);
        Assert.Equal("FILE_MISSING", photos[2].FileHash);
        Assert.Equal("FILE_MISSING", photos[3].FileHash);
        Assert.Equal("FILE_MISSING", photos[4].FileHash);
        Assert.Equal("FILE_MISSING", photos[5].FileHash);
        Assert.Equal("FILE_MISSING", photos[6].FileHash);
    }
}
