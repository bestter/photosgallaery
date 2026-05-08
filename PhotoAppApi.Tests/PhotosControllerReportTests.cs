using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class PhotosControllerReportTests
    {
        private DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private PhotosController CreateController(AppDbContext context, ClaimsPrincipal user)
        {
            var envMock = new Mock<IWebHostEnvironment>();
            var channelMock = new Mock<ChannelWriter<PhotoViewEvent>>();
            var storageMock = new Mock<IObjectStorageService>();

            var controller = new PhotosController(context, envMock.Object, storageMock.Object, channelMock.Object)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                }
            };
            return controller;
        }

        private ClaimsPrincipal CreateUserPrincipal(int userId, string username, string role = "User")
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));
        }

        [Fact]
        public async Task ReportPhoto_HappyPath_PublicPhoto()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", GroupId = null, UploaderUsername = "other_user", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Inappropriate content" };
            var result = await controller.ReportPhoto(1, request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var reportCount = await context.ImageReports.CountAsync();
            Assert.Equal(1, reportCount);
        }

        [Fact]
        public async Task ReportPhoto_HappyPath_GroupPhotoAsMember()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var groupId = Guid.NewGuid();

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", GroupId = groupId, UploaderUsername = "other_user" };
            var userGroup = new UserGroup { UserId = 1, GroupId = groupId, User = new User { Id = 1, Username = "testuser" }, Group = new Group { Id = groupId, Name = "test", ShortName = "test", Description = "test" } };
            context.Photos.Add(photo);
            context.UserGroups.Add(userGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Spam" };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await context.ImageReports.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReportPhoto_HappyPath_GroupPhotoAsAdmin()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var groupId = Guid.NewGuid();

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", GroupId = groupId, UploaderUsername = "other_user", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = CreateUserPrincipal(1, "adminuser", "Admin");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Violence" };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await context.ImageReports.CountAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReportPhoto_NotFound_WhenPhotoDoesNotExist()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Spam" };
            var result = await controller.ReportPhoto(999, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ReportPhoto_BadRequest_WhenUserReportsOwnPhoto()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", UploaderUsername = "testuser", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Spam" };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReportPhoto_BadRequest_WhenReasonIsEmpty()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", UploaderUsername = "other_user", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = " " };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReportPhoto_Forbid_WhenUserNotMemberOfGroup()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var groupId = Guid.NewGuid();

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", GroupId = groupId, UploaderUsername = "other_user", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = CreateUserPrincipal(1, "testuser");
            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Spam" };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ReportPhoto_Unauthorized_WhenUserIdClaimMissingForGroupPhoto()
        {
            var options = CreateNewContextOptions();
            using var context = new AppDbContext(options);
            var groupId = Guid.NewGuid();

            var photo = new Photo { Id = 1, FileName = "test.jpg", Url = "test.jpg", GroupId = groupId, UploaderUsername = "other_user", ThumbnailUrl = string.Empty };
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Create a user without a NameIdentifier claim
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            }, "mock"));

            var controller = CreateController(context, user);

            var request = new ReportDto { Reason = "Spam" };
            var result = await controller.ReportPhoto(1, request);

            Assert.IsType<UnauthorizedResult>(result);
        }
    }
}
