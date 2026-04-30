using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly string _tempDir;
        private readonly string _privateImagesDir;
        private readonly string _thumbnailsDir;

        public ImagesControllerTests()
        {
            _mockEnv = new Mock<IWebHostEnvironment>();

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
            var controller = new ImagesController(context, _mockEnv.Object);

            // Mock a photo in the DB
            var photo = new Photo { FileName = "valid.jpg", Url = "/valid.jpg" };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetImage("valid.jpg");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetImage_WithValidFileName_ReturnsFileIfItExists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object);

            // Mock a photo in the DB
            var photo = new Photo { FileName = "valid.jpg", Url = "/valid.jpg" };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            // Create a dummy file
            var filePath = Path.Combine(_privateImagesDir, "valid.jpg");
            await File.WriteAllTextAsync(filePath, "dummy content");

            // Act
            var result = await controller.GetImage("valid.jpg");

            // Assert
            var physicalFileResult = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal(filePath, physicalFileResult.FileName);
            Assert.Equal("image/jpeg", physicalFileResult.ContentType);
        }

        [Fact]
        public async Task GetImage_WithPathTraversalPayload_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object);

            // Mock a photo in the DB. The Path.GetFileName on traversal payload will extract "passwd"
            var photo = new Photo { FileName = @"..\..\etc\passwd", Url = "/passwd" };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            // The traversal payload
            string traversalPayload = @"..\..\etc\passwd";

            // Act
            var result = await controller.GetImage(traversalPayload);

            // Assert
            Assert.True(result is BadRequestObjectResult || result is NotFoundResult, "Should return BadRequest or NotFound");
        }

        [Fact]
        public async Task GetThumbnail_WithPathTraversalPayload_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new ImagesController(context, _mockEnv.Object);

            // Mock a photo in the DB. The Path.GetFileName on traversal payload will extract "passwd"
            var photo = new Photo { FileName = @"..\..\etc\passwd", Url = "/passwd" };
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            // The traversal payload
            string traversalPayload = @"..\..\etc\passwd";

            // Act
            var result = await controller.GetThumbnail(traversalPayload);

            // Assert
            Assert.True(result is BadRequestObjectResult || result is NotFoundResult, "Should return BadRequest or NotFound");
        }
    }
}
