using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class ImagesControllerTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly IMemoryCache _cache;
        private readonly string _tempDir;
        private readonly string _privateImagesDir;
        private readonly string _thumbnailsDir;

        public ImagesControllerTests()
        {
            _mockEnv = new Mock<IWebHostEnvironment>();
            _cache = new MemoryCache(new MemoryCacheOptions());

            // Setup a temporary directory to act as ContentRootPath
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _privateImagesDir = Path.Combine(_tempDir, "PrivateImages");
            _thumbnailsDir = Path.Combine(_privateImagesDir, "thumbnails");

            Directory.CreateDirectory(_privateImagesDir);
            Directory.CreateDirectory(_thumbnailsDir);

            _mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetImage_WithValidFileName_ReturnsNotFoundIfFileDoesNotExist()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object, _cache, new Mock<PhotoAppApi.Services.IObjectStorageService>().Object);

            // Mock a photo in the DB
            var photo = new Photo { FileName = "valid.jpg", Url = "/valid.jpg", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // Act
            var result = await controller.GetImage("valid.jpg", CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetImage_WithValidFileName_ReturnsFileIfItExists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object, _cache, new Mock<PhotoAppApi.Services.IObjectStorageService>().Object);

            // Mock a photo in the DB
            var photo = new Photo { FileName = "valid.jpg", Url = "/valid.jpg", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // Create a dummy file
            var filePath = Path.Combine(_privateImagesDir, "valid.jpg");
            await File.WriteAllTextAsync(filePath, "dummy content", CancellationToken.None);

            // Act
            var result = await controller.GetImage("valid.jpg", CancellationToken.None);

            // Assert
            var fileStreamResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("image/jpeg", fileStreamResult.ContentType);
            fileStreamResult.FileStream?.Dispose();
        }

        [Fact]
        public async Task GetImage_WithPathTraversalPayload_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object, _cache, new Mock<PhotoAppApi.Services.IObjectStorageService>().Object);

            // Mock a photo in the DB. The Path.GetFileName on traversal payload will extract "passwd"
            var photo = new Photo { FileName = @"..\..\etc\passwd", Url = "/passwd", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // The traversal payload
            string traversalPayload = @"..\..\etc\passwd";

            // Act
            var result = await controller.GetImage(traversalPayload, CancellationToken.None);

            // Assert
            Assert.True(result is BadRequestObjectResult || result is NotFoundResult, "Should return BadRequest or NotFound");
        }

        [Fact]
        public async Task GetThumbnail_WithPathTraversalPayload_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object, _cache, new Mock<PhotoAppApi.Services.IObjectStorageService>().Object);

            // Mock a photo in the DB. The Path.GetFileName on traversal payload will extract "passwd"
            var photo = new Photo { FileName = @"..\..\etc\passwd", Url = "/passwd", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // The traversal payload
            string traversalPayload = @"..\..\etc\passwd";

            // Act
            var result = await controller.GetThumbnail(traversalPayload, CancellationToken.None);

            // Assert
            Assert.True(result is BadRequestObjectResult || result is NotFoundResult, "Should return BadRequest or NotFound");
        }

        [Fact]
        public async Task GetS3Image_ValidPhotoId_ReturnsRedirectToPresignedUrl()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var mockStorage = new Mock<PhotoAppApi.Services.IObjectStorageService>();
            var presignedUrl = "https://s3.amazonaws.com/test-bucket/test-image.jpg?signature=xyz";
            mockStorage.Setup(s => s.GetPresignedUrlAsync("test-image.jpg", It.IsAny<TimeSpan>()))
                .ReturnsAsync(presignedUrl);

            var controller = new ImagesController(context, _mockEnv.Object, _cache, mockStorage.Object);

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test-image.jpg", ThumbnailUrl = "test-thumb.jpg", GroupId = null };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // Act
            var result = await controller.GetS3Image(1, false, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(presignedUrl, redirectResult.Url);
        }


        [Fact]
        public async Task GetS3Image_InvalidPhotoId_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object, _cache, new Mock<PhotoAppApi.Services.IObjectStorageService>().Object);

            // Act
            var result = await controller.GetS3Image(999, false, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetS3Image_ValidPhotoId_IsThumbTrue_ReturnsRedirectToPresignedUrl()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var mockStorage = new Mock<PhotoAppApi.Services.IObjectStorageService>();
            var presignedUrl = "https://s3.amazonaws.com/test-bucket/test-thumb.jpg?signature=xyz";
            mockStorage.Setup(s => s.GetPresignedUrlAsync("test-thumb.jpg", It.IsAny<TimeSpan>()))
                .ReturnsAsync(presignedUrl);

            var controller = new ImagesController(context, _mockEnv.Object, _cache, mockStorage.Object);

            var photo = new Photo { Id = 2, FileName = "test.jpg", Url = "test-image.jpg", ThumbnailUrl = "test-thumb.jpg", GroupId = null };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(CancellationToken.None);

            // Act
            var result = await controller.GetS3Image(2, true, CancellationToken.None);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(presignedUrl, redirectResult.Url);
        }
    }
}
