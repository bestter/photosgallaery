using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class AdminControllerTests
    {
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
            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            var invalidRoleRequest = new RoleUpdateDto { Role = "InvalidRoleName" };

            // Act
            var result = await controller.UpdateUserRole(1, invalidRoleRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Le rôle spécifié n'est pas valide.", badRequestResult.Value);

            // Verify role was not changed in DB
            var dbUser = await context.Users.FindAsync(1);
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
            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            var validRoleRequest = new RoleUpdateDto { Role = "Admin" };

            // Act
            var result = await controller.UpdateUserRole(1, validRoleRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Verify role was changed in DB
            var dbUser = await context.Users.FindAsync(1);
            Assert.Equal(UserRole.Admin, dbUser.Role);
        }

        [Fact]
        public async Task UpdateUserRole_WithNonExistentUser_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDbContext();

            var controller = new AdminController(context);

            var roleRequest = new RoleUpdateDto { Role = "Admin" };

            // Act
            var result = await controller.UpdateUserRole(999, roleRequest);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Utilisateur introuvable.", notFoundResult.Value);
        }
    }
}
