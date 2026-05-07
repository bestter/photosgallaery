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

            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.GetAllUsers();

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
    }
}
