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
    }
}
