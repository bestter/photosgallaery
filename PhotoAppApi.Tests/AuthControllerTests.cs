using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.dtos;
using PhotoAppApi.Models;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoAppApi.Tests
{
    public class AuthControllerTests
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

        private IConfiguration GetConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Jwt:Key"]).Returns("ma_super_cle_secrete_de_64_caracteres_minimum_123!");
            return mockConfig.Object;
        }

        [Fact]
        public async Task Register_NewUser_ReturnsOk()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "password123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Compte créé avec succès !", okResult.Value);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            Assert.NotNull(user);
            Assert.Equal("newuser@example.com", user.Email);
        }

        [Fact]
        public async Task Register_ExistingUser_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            context.Users.Add(new User { Username = "existinguser", Email = "old@example.com", PasswordHash = "hash" });
            await context.SaveChangesAsync();

            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "existinguser",
                Email = "new@example.com",
                Password = "password123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            var message = value.GetType().GetProperty("message").GetValue(value) as string;
            Assert.Equal("Cet usager existe déjà. Veuillez vous connecter ou utiliser un autre nom de compte.", message);
        }

        [Fact]
        public async Task Register_InvalidInviteTokenFormat_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "user",
                Email = "user@example.com",
                Password = "password123",
                InviteToken = "not-a-guid"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            var message = value.GetType().GetProperty("message").GetValue(value) as string;
            Assert.Equal("Le lien d'invitation n'est pas valide.", message);
        }

        [Fact]
        public async Task Register_WithValidPersonalInvite_AddsUserToGroupAndMarksInviteAccepted()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var groupId = Guid.NewGuid();
            var inviteToken = Guid.NewGuid();

            context.Groups.Add(new Group { Id = groupId, Name = "Test Group", ShortName = "test", Description = "desc" });
            context.GroupInvitations.Add(new GroupInvitation
            {
                InviteToken = inviteToken,
                GroupId = groupId,
                Status = "Pending",
                Email = "user@example.com"
            });
            await context.SaveChangesAsync();

            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "user",
                Email = "user@example.com",
                Password = "password123",
                InviteToken = inviteToken.ToString()
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var user = await context.Users.Include(u => u.UserGroups).FirstOrDefaultAsync(u => u.Username == "user");
            Assert.NotNull(user);
            Assert.Single(user.UserGroups);
            Assert.Equal(groupId, user.UserGroups.First().GroupId);

            var invite = await context.GroupInvitations.FirstOrDefaultAsync(i => i.InviteToken == inviteToken);
            Assert.Equal("Accepted", invite.Status);
        }

        [Fact]
        public async Task Register_WithValidGroupInvite_AddsUserToGroup()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var groupId = Guid.NewGuid();
            var inviteToken = Guid.NewGuid();

            context.Groups.Add(new Group
            {
                Id = groupId,
                Name = "Test Group",
                ShortName = "test",
                Description = "desc",
                InviteToken = inviteToken
            });
            await context.SaveChangesAsync();

            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "user",
                Email = "user@example.com",
                Password = "password123",
                InviteToken = inviteToken.ToString()
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var user = await context.Users.Include(u => u.UserGroups).FirstOrDefaultAsync(u => u.Username == "user");
            Assert.NotNull(user);
            Assert.Single(user.UserGroups);
            Assert.Equal(groupId, user.UserGroups.First().GroupId);
        }

        [Fact]
        public async Task Register_Exception_Returns500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Dispose(); // Force exception

            var config = GetConfiguration();
            var controller = new AuthController(context, config);
            var request = new UserRegisterDto
            {
                Username = "user",
                Email = "user@example.com",
                Password = "password123"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var value = statusCodeResult.Value;
            var message = value.GetType().GetProperty("message").GetValue(value) as string;
            Assert.Equal("Une erreur interne est survenue lors de l'enregistrement.", message);
        }
    }
}
