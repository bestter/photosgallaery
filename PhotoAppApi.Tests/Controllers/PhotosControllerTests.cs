using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using Xunit;
using Microsoft.Extensions.Logging;

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
            using var context = new AppDbContext(_dbContextOptions);

            var user = new User { Id = 1, Username = "testuser", PasswordHash = "hash" };
            var photo = new Photo { Id = 1, FileName = "test_image.jpg", UploaderUsername = "testuser", Url = "test" };
            context.Users.Add(user);
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            var envMock = new Mock<IWebHostEnvironment>();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var imagesDir = Path.Combine(tempDir, "PrivateImages");
            var thumbDir = Path.Combine(imagesDir, "thumbnails");
            Directory.CreateDirectory(thumbDir);

            envMock.Setup(e => e.ContentRootPath).Returns(tempDir);

            var filePath = Path.Combine(imagesDir, "test_image.jpg");
            var thumbPath = Path.Combine(thumbDir, "test_image.jpg");
            File.WriteAllText(filePath, "dummy");
            File.WriteAllText(thumbPath, "dummy");

            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var controller = new PhotosController(context, envMock.Object, channelMock.Object);

            var claims = new[] { new Claim(ClaimTypes.Name, "testuser"), new Claim(ClaimTypes.Role, "User") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await controller.DeletePhoto(1);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.False(File.Exists(filePath), "File should be deleted");
            Assert.False(File.Exists(thumbPath), "Thumbnail should be deleted");

            var dbPhoto = await context.Photos.FindAsync(1);
            Assert.Null(dbPhoto);

            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
