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

        [Fact]
        public async Task GetUserPhotos_ShouldFilterOutGroupPhotos_WhenUserIsNotMemberOfGroup()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);

            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            var groupId = Guid.NewGuid();
            context.Photos.Add(new Photo
            {
                Id = 101,
                FileName = "group_photo.jpg",
                UploaderUsername = "targetuser",
                Url = "gallery/group_photo.jpg",
                ThumbnailUrl = "thumbnails/group_photo.jpg",
                GroupId = groupId,
                UploadedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            // Logged in user who is NOT a member of groupId
            var claims = new[] {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.GetUserPhotos("targetuser", 1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            Assert.Empty(photos);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldReturnGroupPhotos_WhenUserIsMemberOfGroup()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);

            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            var groupId = Guid.NewGuid();
            context.Photos.Add(new Photo
            {
                Id = 102,
                FileName = "group_photo.jpg",
                UploaderUsername = "targetuser",
                Url = "gallery/group_photo.jpg",
                ThumbnailUrl = "thumbnails/group_photo.jpg",
                GroupId = groupId,
                UploadedAt = DateTime.UtcNow
            });

            // Make the calling user (Id = 1) a member of groupId
            context.UserGroups.Add(new UserGroup
            {
                UserId = 1,
                GroupId = groupId,
                User = new User { Id = 1, Username = "testuser", PasswordHash = "hash" },
                Group = new Group { Id = groupId, Name = "Test Group", ShortName = "test", Description = "Test" }
            });
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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.GetUserPhotos("targetuser", 1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            var photoList = new System.Collections.Generic.List<Photo>(photos);
            Assert.Single(photoList);
            Assert.Equal(102, photoList[0].Id);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldReturnAllPhotos_WhenCallerIsAdmin()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);

            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            var groupId = Guid.NewGuid();
            context.Photos.Add(new Photo
            {
                Id = 103,
                FileName = "group_photo.jpg",
                UploaderUsername = "targetuser",
                Url = "gallery/group_photo.jpg",
                ThumbnailUrl = "thumbnails/group_photo.jpg",
                GroupId = groupId,
                UploadedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            // Admin calling user
            var claims = new[] {
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.GetUserPhotos("targetuser", 1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            var photoList = new System.Collections.Generic.List<Photo>(photos);
            Assert.Single(photoList);
            Assert.Equal(103, photoList[0].Id);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldCorrectlyMapLikedAndReportedFlags()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);

            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            var photo1 = new Photo { Id = 201, FileName = "photo1.jpg", UploaderUsername = "targetuser", Url = "gallery/photo1.jpg", ThumbnailUrl = "thumbnails/photo1.jpg", UploadedAt = DateTime.UtcNow.AddMinutes(1) };
            var photo2 = new Photo { Id = 202, FileName = "photo2.jpg", UploaderUsername = "targetuser", Url = "gallery/photo2.jpg", ThumbnailUrl = "thumbnails/photo2.jpg", UploadedAt = DateTime.UtcNow.AddMinutes(2) };
            context.Photos.Add(photo1);
            context.Photos.Add(photo2);

            // User with ID 1 likes photo1
            context.PhotoLikes.Add(new PhotoLike
            {
                PhotoId = 201,
                UserId = 1,
                LikedAt = DateTime.UtcNow,
                Photo = null!,
                User = null!
            });

            // User with ID 1 (username: "testuser") reports photo2
            context.ImageReports.Add(new ImageReport
            {
                PhotoId = 202,
                ReporterUsername = "testuser",
                Reason = "Inappropriate",
                ReportedAt = DateTime.UtcNow
            });

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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = await controller.GetUserPhotos("targetuser", 1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            var photoList = new System.Collections.Generic.List<Photo>(photos);

            var p1 = photoList.Find(p => p.Id == 201);
            var p2 = photoList.Find(p => p.Id == 202);

            Assert.NotNull(p1);
            Assert.True(p1.IsLikedByCurrentUser);
            Assert.False(p1.IsReportedByCurrentUser);

            Assert.NotNull(p2);
            Assert.False(p2.IsLikedByCurrentUser);
            Assert.True(p2.IsReportedByCurrentUser);
        }

        [Fact]
        public async Task GetUserPhotos_ShouldOnlyReturnPublicPhotos_WhenCallerIsUnauthenticated()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);

            var targetUser = new User { Id = 2, Username = "targetuser", PasswordHash = "hash" };
            context.Users.Add(targetUser);

            var photo1 = new Photo { Id = 301, FileName = "public_photo.jpg", UploaderUsername = "targetuser", Url = "gallery/public_photo.jpg", ThumbnailUrl = "thumbnails/public_photo.jpg", GroupId = null, UploadedAt = DateTime.UtcNow.AddMinutes(1) };
            var photo2 = new Photo { Id = 302, FileName = "group_photo.jpg", UploaderUsername = "targetuser", Url = "gallery/group_photo.jpg", ThumbnailUrl = "thumbnails/group_photo.jpg", GroupId = Guid.NewGuid(), UploadedAt = DateTime.UtcNow.AddMinutes(2) };
            context.Photos.Add(photo1);
            context.Photos.Add(photo2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();
            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            // Empty principal / claims = unauthenticated caller
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = await controller.GetUserPhotos("targetuser", 1, 20);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var photos = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Photo>>(okResult.Value);
            var photoList = new System.Collections.Generic.List<Photo>(photos);

            // Should return only the public photo (301) and not the group photo (302)
            Assert.Single(photoList);
            Assert.Equal(301, photoList[0].Id);
        }

        [Fact]
        public async Task ToggleLike_ShouldReturnForbid_WhenUserIsNotGroupMember()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var groupId = Guid.NewGuid();
            var photo = new Photo { Id = 1, GroupId = groupId, UploaderUsername = "otheruser", FileName = "test.jpg", Url = "test.jpg", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);

            // The user is not in the group.
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var storageMock = new Mock<IObjectStorageService>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ToggleLike(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ToggleLike_ShouldReturnOk_WhenUserIsGroupMember()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var groupId = Guid.NewGuid();
            var photo = new Photo { Id = 1, GroupId = groupId, UploaderUsername = "otheruser", FileName = "test.jpg", Url = "test.jpg", ThumbnailUrl = string.Empty };

            var userGroup = new UserGroup { UserId = 1, GroupId = groupId };

            context.Photos.Add(photo);
            context.UserGroups.Add(userGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var storageMock = new Mock<IObjectStorageService>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ToggleLike(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }
        [Fact]
        public async Task ToggleLike_ShouldReturnOk_WhenUserIsAdminAndNotGroupMember()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var groupId = Guid.NewGuid();
            var photo = new Photo { Id = 1, GroupId = groupId, UploaderUsername = "otheruser", FileName = "test.jpg", Url = "test.jpg", ThumbnailUrl = string.Empty };

            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            var storageMock = new Mock<IObjectStorageService>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.ToggleLike(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
        }


        [Fact]
        public async Task GenerateMissingThumbnails_ShouldGenerateThumbnails_WhenOriginalExistsAndThumbIsMissing()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var imagesDir = Path.Combine(tempDir, "images");
            var thumbDir = Path.Combine(imagesDir, "thumbnails");
            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(thumbDir);

            var photo1 = new Photo { Id = 1, FileName = "photo1.jpg" };
            var photo2 = new Photo { Id = 2, FileName = "photo2.jpg" };

            // Create fake original image
            var fakeImageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x60, 0x00, 0x60, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xDB, 0x00, 0x43, 0x01, 0x09, 0x09, 0x09, 0x0C, 0x0B, 0x0C, 0x18, 0x0D, 0x0D, 0x18, 0x32, 0x21, 0x1C, 0x21, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x0A, 0x00, 0x0A, 0x03, 0x01, 0x22, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03, 0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D, 0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xC4, 0x00, 0x1F, 0x01, 0x00, 0x03, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x11, 0x00, 0x02, 0x01, 0x02, 0x04, 0x04, 0x03, 0x04, 0x07, 0x05, 0x04, 0x04, 0x00, 0x01, 0x02, 0x77, 0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71, 0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0, 0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34, 0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xDA, 0x00, 0x0C, 0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00, 0xFD, 0xFC, 0xA2, 0x8A, 0x28, 0xAF, 0xFF, 0xD9 };
            File.WriteAllBytes(Path.Combine(imagesDir, "photo1.jpg"), fakeImageBytes);
            File.WriteAllBytes(Path.Combine(imagesDir, "photo2.jpg"), fakeImageBytes);

            // Create fake thumbnail only for photo2
            File.WriteAllBytes(Path.Combine(thumbDir, "photo2.jpg"), fakeImageBytes);

            using var context = new AppDbContext(_dbContextOptions);
            context.Photos.Add(photo1);
            context.Photos.Add(photo2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(tempDir);
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.GenerateMissingThumbnails();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Check that the response contains the right counts
            var type = okResult.Value.GetType();
            var message = type.GetProperty("message")?.GetValue(okResult.Value, null) as string;
            var created = (int?)type.GetProperty("miniaturesCreees")?.GetValue(okResult.Value, null);
            var missing = (int?)type.GetProperty("imagesOriginalesIntrouvables")?.GetValue(okResult.Value, null);

            Assert.Equal("Opération de maintenance terminée avec succès.", message);
            Assert.Equal(1, created);
            Assert.Equal(0, missing);

            Assert.True(File.Exists(Path.Combine(thumbDir, "photo1.jpg")));
            Assert.True(File.Exists(Path.Combine(thumbDir, "photo2.jpg")));

            // Clean up
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task GenerateMissingThumbnails_ShouldReportMissingOriginals_WhenOriginalIsMissing()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var imagesDir = Path.Combine(tempDir, "images");
            var thumbDir = Path.Combine(imagesDir, "thumbnails");
            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(thumbDir);

            var photo1 = new Photo { Id = 1, FileName = "photo1.jpg" };

            // No original is created

            using var context = new AppDbContext(_dbContextOptions);
            context.Photos.Add(photo1);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(tempDir);
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.GenerateMissingThumbnails();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var type = okResult.Value.GetType();
            var created = (int?)type.GetProperty("miniaturesCreees")?.GetValue(okResult.Value, null);
            var missing = (int?)type.GetProperty("imagesOriginalesIntrouvables")?.GetValue(okResult.Value, null);

            Assert.Equal(0, created);
            Assert.Equal(1, missing);

            Assert.False(File.Exists(Path.Combine(thumbDir, "photo1.jpg")));

            // Clean up
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task GenerateMissingThumbnails_ShouldReturn500_WhenExceptionOccurs()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var imagesDir = Path.Combine(tempDir, "images");
            var thumbDir = Path.Combine(imagesDir, "thumbnails");
            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(thumbDir);

            var photo1 = new Photo { Id = 1, FileName = "photo1.jpg" };

            // Create an INVALID original image (e.g., text file) that ImageSharp will fail to load
            File.WriteAllText(Path.Combine(imagesDir, "photo1.jpg"), "Not a valid image");

            using var context = new AppDbContext(_dbContextOptions);
            context.Photos.Add(photo1);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(tempDir);
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "adminuser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.GenerateMissingThumbnails();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Erreur interne lors de la compression des images.", statusCodeResult.Value);

            // Clean up
            Directory.Delete(tempDir, true);
        }

    }
}