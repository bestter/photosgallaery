using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class GroupsControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = string.Empty,
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsNull()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = null!,
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsWhitespace()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = "   ",
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task DeleteGroup_ReturnsNotFound_WhenGroupDoesNotExist()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await controller.DeleteGroup(nonExistentId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            // The controller returns: return NotFound(new { message = "Groupe non trouvé." });
            // We can check the message if needed
        }

        [Fact]
        public async Task DeleteGroup_ReturnsOk_WhenGroupExists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Test Group",
                ShortName = "test-group",
                Description = "A test group"
            };
            context.Groups.Add(group);
            await context.SaveChangesAsync();
            var controller = new GroupsController(context);

            // Act
            var result = await controller.DeleteGroup(group.Id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var deletedGroup = await context.Groups.FindAsync(group.Id);
            Assert.Null(deletedGroup);
        }
    }
}
// Verified empty name tests exist and pass
