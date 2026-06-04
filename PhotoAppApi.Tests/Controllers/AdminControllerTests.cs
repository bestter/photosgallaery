using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public void GetAllUsers_HasAuthorizeAttribute_WithAdminRole()
        {
            // Arrange
            var methodInfo = typeof(AdminController).GetMethod(nameof(AdminController.GetAllUsers));

            // Act
            var authorizeAttribute = methodInfo.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            // Checking if [Authorize(Roles = "Admin")] attribute is present on the method itself.
            // If the method has no attribute, we check the class.
            if (authorizeAttribute == null)
            {
                var classAuthorizeAttribute = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();
                Assert.NotNull(classAuthorizeAttribute);
                Assert.Equal("Admin", classAuthorizeAttribute.Roles);
            }
            else
            {
                Assert.NotNull(authorizeAttribute);
                Assert.Equal("Admin", authorizeAttribute.Roles);
            }
        }

        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task UpdateUserRole_WithInvalidRole_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDbContext();

            // Create a test user
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.User
            };
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            var invalidRoleRequest = new RoleUpdateDto { Role = "InvalidRoleName" };

            // Act
            var result = await controller.UpdateUserRole(1, invalidRoleRequest, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Le rôle spécifié n'est pas valide.", badRequestResult.Value);

            // Verify role was not changed in DB
            var dbUser = await context.Users.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
            Assert.Equal(UserRole.User, dbUser.Role);
        }

        [Fact]
        public async Task UpdateUserRole_WithValidRole_ReturnsOk()
        {
            // Arrange
            using var context = GetDbContext();

            // Create a test user
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.User
            };
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            var validRoleRequest = new RoleUpdateDto { Role = "Admin" };

            // Act
            var result = await controller.UpdateUserRole(1, validRoleRequest, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Verify role was changed in DB
            var dbUser = await context.Users.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
            Assert.Equal(UserRole.Admin, dbUser.Role);
        }

        [Fact]
        public async Task UpdateUserRole_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDbContext();


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            var roleRequest = new RoleUpdateDto { Role = "Admin" };

            // Act
            var result = await controller.UpdateUserRole(999, roleRequest, TestContext.Current.CancellationToken);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Utilisateur introuvable.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            using var context = GetDbContext();

            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetAllUsers(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsAllUsers_WithRoleAndGroups()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = "TestGroup",
                ShortName = "TG",
                Description = "A test group"
            };

            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Role = UserRole.User
            };

            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Role = UserRole.Admin
            };

            context.Groups.Add(group);
            context.Users.Add(user1);
            context.Users.Add(user2);

            var userGroup = new UserGroup
            {
                UserId = user1.Id,
                User = user1,
                GroupId = group.Id,
                Group = group,
                Role = GroupUserRole.Member
            };
            context.UserGroups.Add(userGroup);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetAllUsers(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Use dynamic or reflection to inspect anonymous types
            var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var usersList = items.ToList();
            Assert.Equal(2, usersList.Count);

            // Validate user1
            var user1Result = usersList.First(u => (int)u.GetType().GetProperty("Id").GetValue(u) == 1);
            Assert.Equal("user1", user1Result.GetType().GetProperty("Username").GetValue(user1Result));
            Assert.Equal("User", user1Result.GetType().GetProperty("Role").GetValue(user1Result));

            var groups1 = (List<string>)user1Result.GetType().GetProperty("Groups").GetValue(user1Result);
            Assert.Single(groups1);
            Assert.Equal("TestGroup", groups1[0]);

            // Validate user2
            var user2Result = usersList.First(u => (int)u.GetType().GetProperty("Id").GetValue(u) == 2);
            Assert.Equal("user2", user2Result.GetType().GetProperty("Username").GetValue(user2Result));
            Assert.Equal("Admin", user2Result.GetType().GetProperty("Role").GetValue(user2Result));

            var groups2 = (List<string>)user2Result.GetType().GetProperty("Groups").GetValue(user2Result);
            Assert.Empty(groups2);
        }

        [Fact]
        public async Task GetAllUsers_WhenExceptionOccurs_ReturnsStatusCode500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            // Dispose the context immediately so any query throws an ObjectDisposedException
            context.Dispose();


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetAllUsers(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            // Validate the message
            Assert.Equal("Internal server error", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetReports_ReturnsEmptyList_WhenNoReportsExist()
        {
            // Arrange
            using var context = GetDbContext();

            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetReports(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Empty(items);
        }

        [Fact]
        public async Task GetReports_ReturnsReports_JoinedWithPhotos()
        {
            // Arrange
            using var context = GetDbContext();

            var photo = new Photo
            {
                Id = 1,
                FileName = "test.jpg",
                Url = "/uploads/test.jpg",
                UploaderUsername = "uploader1", ThumbnailUrl = string.Empty
            };

            var report = new ImageReport
            {
                Id = 1,
                PhotoId = 1,
                Reason = "Inappropriate content",
                Status = "Pending",
                ReportedAt = DateTime.UtcNow
            };

            context.Photos.Add(photo);
            context.ImageReports.Add(report);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetReports(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Use dynamic or reflection to inspect anonymous types
            var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            var reportsList = items.ToList();
            Assert.Single(reportsList);

            var firstReport = reportsList[0];
            Assert.Equal(1, firstReport.GetType().GetProperty("ReportId").GetValue(firstReport));
            Assert.Equal(1, firstReport.GetType().GetProperty("PhotoId").GetValue(firstReport));
            Assert.Equal("/uploads/test.jpg", firstReport.GetType().GetProperty("PhotoUrl").GetValue(firstReport));
            Assert.Equal("uploader1", firstReport.GetType().GetProperty("Uploader").GetValue(firstReport));
            Assert.Equal("Inappropriate content", firstReport.GetType().GetProperty("Reason").GetValue(firstReport));
            Assert.Equal("Pending", firstReport.GetType().GetProperty("Status").GetValue(firstReport));
        }

        [Fact]
        public async Task GetReports_WhenExceptionOccurs_ReturnsStatusCode500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            // Dispose the context immediately so any query throws an ObjectDisposedException
            context.Dispose();


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.GetReports(search: null, page: 1, pageSize: 20, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            // Validate the message via reflection
            var message = statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value) as string;
            Assert.Equal("Erreur lors de la récupération des signalements.", message);
        }

        [Fact]
        public async Task DeleteReport_WithValidId_UpdatesStatusToProcessed()
        {
            // Arrange
            using var context = GetDbContext();

            var report = new ImageReport
            {
                Id = 1,
                PhotoId = 1,
                Reason = "Test report",
                Status = "Pending"
            };

            context.ImageReports.Add(report);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.DeleteReport(1, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Validate the message via reflection
            var message = okResult.Value.GetType().GetProperty("message").GetValue(okResult.Value) as string;
            Assert.Equal("Le signalement a été marqué comme traité.", message);

            // Verify status was updated
            var dbReport = await context.ImageReports.FindAsync(new object[] { 1 }, TestContext.Current.CancellationToken);
            Assert.Equal("Processed", dbReport.Status);
        }

        [Fact]
        public async Task DeleteReport_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDbContext();

            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.DeleteReport(999, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteReport_WhenExceptionOccurs_ReturnsStatusCode500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);
            context.Dispose(); // Will cause ObjectDisposedException


            var httpContext = new DefaultHttpContext();
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            var controller = new AdminController(context, new Mock<ILogger<AdminController>>().Object)
            {
                ControllerContext = controllerContext
            };


            // Act
            var result = await controller.DeleteReport(1, TestContext.Current.CancellationToken);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            // Validate the message via reflection
            var message = statusCodeResult.Value.GetType().GetProperty("message").GetValue(statusCodeResult.Value) as string;
            Assert.Equal("Une erreur interne est survenue lors de l'effacement du signalement.", message);
        }
    }
}
