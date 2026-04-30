using Microsoft.AspNetCore.Mvc;
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
            await context.SaveChangesAsync();

            var controller = new GroupRequestsController(context);

            // Act
            var result = await controller.GetAllGroupRequests();

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
        public async Task GetAllGroupRequests_WhenExceptionOccurs_Returns500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Dispose(); // Dispose to cause ObjectDisposedException when queried

            var controller = new GroupRequestsController(context);

            // Act
            var result = await controller.GetAllGroupRequests();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var message = value.GetType().GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Erreur lors de la récupération des demandes.", message);
        }
    }
}
