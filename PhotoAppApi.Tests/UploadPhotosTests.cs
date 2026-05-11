using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Channels;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class UploadPhotosTests
    {
        private DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task UploadPhotos_Should_Reject_Duplicate_FileHash()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            // Mock IWebHostEnvironment
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            // Mock ChannelWriter
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            // Setup a fake User
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object)  
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            // Generate a dummy file
            var fileBytes = System.Convert.FromHexString("89504E470D0A1A0A0000000D49484452000000010000000108060000001F15C4890000000A49444154789C63000100000500010D0A2DB40000000049454E44AE426082");
            using var sha512 = SHA512.Create();
            var hashBytes = sha512.ComputeHash(fileBytes);
            var fileHash = Convert.ToHexStringLower(hashBytes);

            // Pre-populate db with the hash
            context.Photos.Add(new Photo
            {
                FileName = "existing_image.jpg",
                Url = "/api/images/existing_image.jpg",
                UploaderUsername = "anotheruser",
                FileHash = fileHash,
                    UploadedAt = DateTime.UtcNow,
                    ThumbnailUrl = "/api/images/existing_image_thumbnail.jpg",
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Create fake IFormFile
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.Length).Returns(fileBytes.Length);
            formFileMock.Setup(f => f.FileName).Returns("new_image.jpg");
            formFileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileBytes));

            var files = new List<IFormFile> { formFileMock.Object };
            var tags = JsonSerializer.Serialize(new List<string> { "tag1" });

            var moderationMock = new Mock<IModerationService>();
            moderationMock.Setup(m => m.CheckImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModerationResult { IsNsfw = false, SafeScore = 0.99, Label = "safe" });

            // Act
            var result = await controller.UploadPhotos(files, moderationMock.Object, tags, null, false, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var value = okResult.Value;
            var json = JsonSerializer.Serialize(value);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("erreurs"));

            var errorsElement = dict["erreurs"];
            var errorsList = JsonSerializer.Deserialize<List<string>>(errorsElement.GetRawText());

            Assert.NotNull(errorsList);
            Assert.Contains(errorsList, e => e.Contains("new_image.jpg"));

            // Verify it was not added again
            var photosInDb = await context.Photos.ToListAsync(TestContext.Current.CancellationToken);
            Assert.Single(photosInDb);
        }

        [Fact]
        public async Task UploadPhotos_Should_Reject_Duplicate_FileHash_In_Same_Batch()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));
            
            var storageMock = new Mock<IObjectStorageService>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            var fileBytes = System.Convert.FromHexString("47494638396101000100800000000000ffffff21f90401000000002c000000000100010000020144003b");

            var formFileMock1 = new Mock<IFormFile>();
            formFileMock1.Setup(f => f.Length).Returns(fileBytes.Length);
            formFileMock1.Setup(f => f.FileName).Returns("img1.jpg");
            formFileMock1.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileBytes));

            var formFileMock2 = new Mock<IFormFile>();
            formFileMock2.Setup(f => f.Length).Returns(fileBytes.Length);
            formFileMock2.Setup(f => f.FileName).Returns("img2.jpg");
            formFileMock2.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileBytes));

            var files = new List<IFormFile> { formFileMock1.Object, formFileMock2.Object };
            var tags = JsonSerializer.Serialize(new List<string> { "tag1" });

            var moderationMock = new Mock<IModerationService>();
            moderationMock.Setup(m => m.CheckImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModerationResult { IsNsfw = false, SafeScore = 0.99, Label = "safe" });

            storageMock.Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("dummy_url");

            // Act
            var result = await controller.UploadPhotos(files, moderationMock.Object, tags, null, false,TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var json = JsonSerializer.Serialize(okResult.Value);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var errorsList = JsonSerializer.Deserialize<List<string>>(dict?["erreurs"].GetRawText() ?? "[]");

            Assert.Contains(errorsList, e => e.Contains("img2.jpg") && e.Contains("double"));

            var photosInDb = await context.Photos.ToListAsync(TestContext.Current.CancellationToken);
            Assert.Single(photosInDb);
        }

        [Fact]
        public async Task UploadPhotos_Should_Reject_Nsfw_Image()
        {
            // Arrange
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };

            var fileBytes = System.Convert.FromHexString("89504E470D0A1A0A0000000D49484452000000010000000108060000001F15C4890000000A49444154789C63000100000500010D0A2DB40000000049454E44AE426082");

            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.Length).Returns(fileBytes.Length);
            formFileMock.Setup(f => f.FileName).Returns("nsfw_image.jpg");
            formFileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(fileBytes));

            var files = new List<IFormFile> { formFileMock.Object };
            var tags = JsonSerializer.Serialize(new List<string> { "tag1" });

            var moderationMock = new Mock<IModerationService>();
            moderationMock.Setup(m => m.CheckImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ModerationResult { IsNsfw = true, NsfwScore = 0.95, SafeScore = 0.05, Label = "nsfw" });

            // Act
            var result = await controller.UploadPhotos(files, moderationMock.Object, tags, null, false);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            var json = JsonSerializer.Serialize(value);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            Assert.NotNull(dict);
            Assert.True(dict.ContainsKey("message"));
            Assert.Contains("Image contains inappropriate content", dict["message"].GetString() ?? string.Empty);
            
            var photosInDb = await context.Photos.ToListAsync(TestContext.Current.CancellationToken);
            Assert.Empty(photosInDb);
        }
    }
}
