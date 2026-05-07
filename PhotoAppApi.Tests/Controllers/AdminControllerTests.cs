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
        public async Task GetAllUsers_ReturnsAllUsersWithCorrectData()
        {
            // Arrange
            using var context = GetDbContext();

            // Create groups
            var groupA = new Group { Id = Guid.NewGuid(), Name = "Group A", ShortName = "group-a" };
            context.Groups.Add(groupA);

            // Create users
            var adminUser = new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@example.com",
                Role = UserRole.Admin
            };
            var regularUser = new User
            {
                Id = 2,
                Username = "user",
                Email = "user@example.com",
                Role = UserRole.User
            };
            context.Users.AddRange(adminUser, regularUser);

            // Add admin to Group A
            context.UserGroups.Add(new UserGroup { UserId = 1, GroupId = groupA.Id });

            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);

            var userList = new List<object>();
            foreach (var u in users) userList.Add(u);

            Assert.Equal(2, userList.Count);

            // Use reflection to access properties of anonymous types from another assembly
            var firstUser = userList.FirstOrDefault(u => (int)u.GetType().GetProperty("Id").GetValue(u) == 1);
            Assert.NotNull(firstUser);
            Assert.Equal("admin", firstUser.GetType().GetProperty("Username").GetValue(firstUser));
            Assert.Equal("Admin", firstUser.GetType().GetProperty("Role").GetValue(firstUser));
            var groups = Assert.IsAssignableFrom<IEnumerable<string>>(firstUser.GetType().GetProperty("Groups").GetValue(firstUser));
            Assert.Contains("Group A", groups);

            var secondUser = userList.FirstOrDefault(u => (int)u.GetType().GetProperty("Id").GetValue(u) == 2);
            Assert.NotNull(secondUser);
            Assert.Equal("user", secondUser.GetType().GetProperty("Username").GetValue(secondUser));
            Assert.Equal("User", secondUser.GetType().GetProperty("Role").GetValue(secondUser));
            var secondGroups = Assert.IsAssignableFrom<IEnumerable<string>>(secondUser.GetType().GetProperty("Groups").GetValue(secondUser));
            Assert.Empty(secondGroups);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new AdminController(context);

            // Act
            var result = await controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var users = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            Assert.Empty(users);
        }
    }
}
