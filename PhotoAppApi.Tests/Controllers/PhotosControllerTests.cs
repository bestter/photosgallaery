using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class PhotosControllerTests
    {
        private DbContextOptions<AppDbContext> _dbContextOptions;

        public PhotosControllerTests()
        {
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task DeletePhoto_ShouldDeleteFileAndRecord()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var imagesDir = Path.Combine(tempDir, "PrivateImages");
            var thumbDir = Path.Combine(imagesDir, "thumbnails");
            
            var filePath = Path.Combine(imagesDir, "test_image.jpg");
            var thumbPath = Path.Combine(thumbDir, "test_image.jpg");

            try
            {
                using var context = new AppDbContext(_dbContextOptions);

                var user = new User { Id = 1, Username = "testuser", PasswordHash = "hash" };
                var photo = new Photo 
                { 
                    Id = 1, 
                    FileName = "test_image.jpg", 
                    UploaderUsername = "testuser", 
                    Url = "gallery/test_image.jpg", 
                    ThumbnailUrl = "thumbnails/test_image.jpg" 
                };
                context.Users.Add(user);
                context.Photos.Add(photo);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);

                var envMock = new Mock<IWebHostEnvironment>();
                Directory.CreateDirectory(thumbDir);
                envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

                File.WriteAllText(filePath, "dummy");
                File.WriteAllText(thumbPath, "dummy");

                var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
                var storageMock = new Mock<IObjectStorageService>();
                
                // Set up S3 deletion mock behavior
                storageMock.Setup(s => s.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.CompletedTask);

                var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

                var claims = new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "User") };
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                var claimsPrincipal = new ClaimsPrincipal(identity);

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                };

                // Act
                var result = await controller.DeletePhoto(1, TestContext.Current.CancellationToken);

                // Assert
                Assert.IsType<OkObjectResult>(result);
                Assert.False(File.Exists(filePath), "Local file should be deleted");
                Assert.False(File.Exists(thumbPath), "Local thumbnail should be deleted");

                // Verify S3 deletion was invoked for both original and thumbnail
                storageMock.Verify(s => s.DeleteImageAsync("gallery/test_image.jpg", It.IsAny<CancellationToken>()), Times.Once);
                storageMock.Verify(s => s.DeleteImageAsync("thumbnails/test_image.jpg", It.IsAny<CancellationToken>()), Times.Once);

                // Assert database record deletion using a fresh context instance to bypass local change tracker cache
                using (var verifyContext = new AppDbContext(_dbContextOptions))
                {
                    var dbPhoto = await verifyContext.Photos.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
                    Assert.Null(dbPhoto);
                }
            }
            finally
            {
                // Cleanup temp directories even if assertions fail or exceptions occur
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void GetImageUrl_ShouldReturnLocalProxyRoute()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();

            int photoId = 123;
            string expectedUrl = $"/api/images/s3/{photoId}?isThumb=false";

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            // Act
            var result = controller.GetImageUrl(photoId, false);

            // Assert
            Assert.Equal(expectedUrl, result);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldReturnNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var claims = new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "User") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.GetUserPhotos("nonexistentuser", 1, 20);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldReturnPaginatedPhotosAndTotalCountHeader()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            
            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            // Add 25 photos for targetuser
            for (int i = 1; i <= 25; i++)
            {
                context.Photos.Add(new Photo
                {
                    Id = i,
                    FileName = $"photo{i}.jpg",
                    UploaderUsername = "targetuser",
                    Url = $"gallery/photo{i}.jpg",
                    ThumbnailUrl = $"thumbnails/photo{i}.jpg",
                    UploadedAt = DateTime.UtcNow.AddMinutes(i)
                });
            }
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var claims = new[] { 
                new Claim(ClaimTypes.Name, "testuser"), 
                new Claim(ClaimTypes.NameIdentifier, "1"), 
                new Claim(ClaimTypes.Role, "User") 
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act: request page 2, page size 10
            var result = await controller.GetUserPhotos("targetuser", page: 2, pageSize: 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            
            // Should return exactly 10 photos
            var photoList = new System.Collections.Generic.List<Photo>(photos);
            Assert.Equal(10, photoList.Count);

            // Header should have total count of 25
            Assert.True(httpContext.Response.Headers.TryGetValue("X-Total-Count", out var totalCountHeader));
            Assert.Equal("25", totalCountHeader.ToString());
        }
    }
}
