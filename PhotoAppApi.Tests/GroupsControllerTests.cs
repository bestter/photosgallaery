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
    }
}
