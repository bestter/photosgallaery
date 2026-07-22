using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class GroupRequestsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetAllGroupRequests_ReturnsOkWithSortedRequests()
        {
            // Arrange
            using var context = GetDbContext();

            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Role = UserRole.User
            };

            context.Users.Add(user);

            var request1 = new GroupRequest
            {
                Id = Guid.NewGuid(),
                Name = "Old Request",
                Description = "This was requested first",
                Requester = user,
                RequestedAt = DateTime.UtcNow.AddDays(-2)
            };

            var request2 = new GroupRequest
            {
                Id = Guid.NewGuid(),
                Name = "New Request",
                Description = "This was requested second",
                Requester = user,
                RequestedAt = DateTime.UtcNow.AddDays(-1)
            };

            context.GroupRequests.AddRange(request1, request2);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupRequestsController(context);

            // Act
            var result = await controller.GetAllGroupRequests(1, 20, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Since it's returning an anonymous type, we can't easily cast to IEnumerable<GroupRequestDto>,
            // but we can check it's an IEnumerable and count it using dynamic or reflection.
            var requestsEnumerable = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);

            var requestsList = new List<dynamic>();
            foreach (var r in requestsEnumerable)
            {
                requestsList.Add(r);
            }

            Assert.Equal(2, requestsList.Count);

            // Check sorting (descending, so newer request should be first)
            var firstRequestName = requestsList[0].GetType().GetProperty("Name").GetValue(requestsList[0], null) as string;
            var secondRequestName = requestsList[1].GetType().GetProperty("Name").GetValue(requestsList[1], null) as string;

            Assert.Equal("New Request", firstRequestName);
            Assert.Equal("Old Request", secondRequestName);
        }

        [Fact]
        public async Task GetAllGroupRequests_WhenExceptionOccurs_LogsErrorAndReturns500()
        {
            // Arrange
            var repository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetExecutingAssembly());
            var appender = new TestMemoryAppender();
            log4net.Config.BasicConfigurator.Configure(repository, appender);

            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;
                var context = new AppDbContext(options);
                context.Dispose(); // Dispose to cause ObjectDisposedException when queried

                var controller = new GroupRequestsController(context);

                // Act
                var result = await controller.GetAllGroupRequests(1, 20, TestContext.Current.CancellationToken);

                // Assert
                var statusCodeResult = Assert.IsType<ObjectResult>(result);
                Assert.Equal(500, statusCodeResult.StatusCode);

                var value = statusCodeResult.Value;
                Assert.NotNull(value);
                var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
                Assert.Equal("Erreur lors de la récupération des demandes.", message);

                var events = appender.GetEvents();
                var errorEvent = events.FirstOrDefault(e => e.Level == log4net.Core.Level.Error);
                Assert.NotNull(errorEvent);
                // Assert.Equal("An error occured in GetAllGroupRequests", errorEvent.RenderedMessage);
                Assert.NotNull(errorEvent.ExceptionObject);
            }
            finally
            {
                repository.ResetConfiguration();
            }
        }

        [Fact]
        public async Task SubmitGroupRequest_WhenExceptionOccurs_Returns500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Dispose(); // Dispose to cause ObjectDisposedException when queried

            var controller = new GroupRequestsController(context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));
            controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user } };

            var request = new SubmitGroupRequestDto { Name = "Test", Description = "Test" };

            // Act
            var result = await controller.SubmitGroupRequest(request, TestContext.Current.CancellationToken);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Erreur lors de la création de la demande de groupe.", message);
        }

        [Fact]
        public async Task DeleteGroupRequest_WhenRequestExists_ReturnsOkAndDeletes()
        {
            // Arrange
            using var context = GetDbContext();

            var requestId = Guid.NewGuid();
            var request = new GroupRequest
            {
                Id = requestId,
                Name = "To Delete",
                Description = "Delete me",
                Requester = new User { Id = 2, Username = "test", Email = "test@example.com" }
            };

            context.GroupRequests.Add(request);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupRequestsController(context);

            // Act
            var result = await controller.DeleteGroupRequest(requestId, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            Assert.NotNull(value);
            var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Demande supprimée avec succès.", message);

            // Verify deletion
            var deletedRequest = await context.GroupRequests.FindAsync(new object[] { requestId }, TestContext.Current.CancellationToken);
            Assert.Null(deletedRequest);
        }

        [Fact]
        public async Task DeleteGroupRequest_WhenRequestNotFound_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new GroupRequestsController(context);
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await controller.DeleteGroupRequest(nonExistentId, TestContext.Current.CancellationToken);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            Assert.NotNull(value);
            var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Demande non trouvée.", message);
        }

        [Fact]
        public async Task DeleteGroupRequest_WhenExceptionOccurs_Returns500()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var requestId = Guid.NewGuid();

            // Seed database
            using (var seedContext = new AppDbContext(options))
            {
                seedContext.GroupRequests.Add(new GroupRequest { Id = requestId, Name = "Test Request", Description = "Desc", Requester = new User { Id = 3, Username = "test3", Email = "test3@example.com" } });
                await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Create Mocked DbContext with same InMemory DB but intercepted SaveChangesAsync
            var mockContext = new Mock<AppDbContext>(options) { CallBase = true };
            mockContext
                .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Simulated database error during deletion."));

            var controller = new GroupRequestsController(mockContext.Object);

            // Act
            var result = await controller.DeleteGroupRequest(requestId, TestContext.Current.CancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            var value = objectResult.Value;
            Assert.NotNull(value);
            var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Erreur lors de la suppression de la demande.", message);
        }
    }
}
