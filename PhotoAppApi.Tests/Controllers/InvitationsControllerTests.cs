using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class InvitationsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private InvitationsController CreateController(AppDbContext context, IEmailService emailService, int? userId = null, string? username = null, string? role = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"FrontendUrl\": \"http://localhost:5173\"}")))
                .Build();

            var controller = new InvitationsController(context, emailService, configuration);

            if (userId.HasValue || username != null || role != null)
            {
                var claims = new System.Collections.Generic.List<Claim>();

                if (userId.HasValue)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
                }

                if (username != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, username));
                }

                if (role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "TestAuthType");
                var user = new ClaimsPrincipal(identity);

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
            }
            else
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };
            }

            return controller;
        }

        [Fact]
        public async Task CreateInvitation_WithoutValidUserId_ReturnsUnauthorized()
        {
            // Arrange
            using var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object); // No user context

            var dto = new CreateInvitationDto { GroupId = Guid.NewGuid(), Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task CreateInvitation_WithNonExistentGroup_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDbContext();
            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "inviter");

            var dto = new CreateInvitationDto { GroupId = Guid.NewGuid(), Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var value = notFoundResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Cercle introuvable.", message);
        }

        [Fact]
        public async Task CreateInvitation_WhenUserNotMemberOfGroup_ReturnsForbid()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            context.Groups.Add(group);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "inviter", role: "User"); // Not admin, not member

            var dto = new CreateInvitationDto { GroupId = group.Id, Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateInvitation_WithExistingUserAlreadyInGroup_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            var existingUser = new User { Id = 2, Username = "existing", Email = "test@example.com" };
            context.Groups.Add(group);
            context.Users.Add(existingUser);
            context.UserGroups.Add(new UserGroup { UserId = 2, GroupId = group.Id }); // User is already in the group
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "admin", role: "Admin"); // Admin can invite anyone

            var dto = new CreateInvitationDto { GroupId = group.Id, Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Cet utilisateur fait déjà partie du cercle.", message);
        }

        [Fact]
        public async Task CreateInvitation_WithExistingUserNotInGroup_AddsToGroupAndReturnsOk()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            var existingUser = new User { Id = 2, Username = "existing", Email = "test@example.com" };
            context.Groups.Add(group);
            context.Users.Add(existingUser);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "admin", role: "Admin");

            var dto = new CreateInvitationDto { GroupId = group.Id, Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("L'utilisateur existant a été ajouté automatiquement au cercle !", message);

            var isMember = await context.UserGroups.AnyAsync(ug => ug.UserId == 2 && ug.GroupId == group.Id, TestContext.Current.CancellationToken);
            Assert.True(isMember);

            // Verify email was NOT sent
            emailServiceMock.Verify(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task CreateInvitation_WithPendingInvitation_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            context.Groups.Add(group);

            var existingInvite = new GroupInvitation
            {
                GroupId = group.Id,
                InviterId = 1,
                Email = "test@example.com",
                Status = "Pending",
                InviteToken = Guid.NewGuid()
            };
            context.GroupInvitations.Add(existingInvite);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "admin", role: "Admin");

            var dto = new CreateInvitationDto { GroupId = group.Id, Email = "test@example.com" };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequestResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Une invitation est déjà en attente pour cette adresse e-mail dans ce cercle.", message);
        }

        [Fact]
        public async Task CreateInvitation_WithNewUser_CreatesInvitationSendsEmailAndReturnsOk()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            context.Groups.Add(group);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "admin", role: "Admin");

            var dto = new CreateInvitationDto
            {
                GroupId = group.Id,
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                Message = "Join us!"
            };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal($"Invitation envoyée avec succès à {dto.Email} !", message);

            var invitation = await context.GroupInvitations.FirstOrDefaultAsync(i => i.Email == dto.Email, TestContext.Current.CancellationToken);
            Assert.NotNull(invitation);
            Assert.Equal("Pending", invitation.Status);
            Assert.Equal(1, invitation.InviterId);

            // Verify email WAS sent
            emailServiceMock.Verify(x => x.SendInvitationEmailAsync(
                dto.Email,
                dto.FirstName,
                dto.LastName,
                "admin",
                "Test Group",
                dto.Message,
                It.Is<string>(url => url.Contains(invitation.InviteToken.ToString())),
                It.IsAny<CancellationToken>()
            ), Times.Once());
        }

        [Fact]
        public async Task CreateInvitation_WhenEmailServiceThrows_Returns500()
        {
            // Arrange
            using var context = GetDbContext();

            var group = new Group { Id = Guid.NewGuid(), Name = "Test Group", ShortName = "TG", Description = "Test" };
            context.Groups.Add(group);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            )).ThrowsAsync(new Exception("Email service failed"));

            var controller = CreateController(context, emailServiceMock.Object, userId: 1, username: "admin", role: "Admin");

            var dto = new CreateInvitationDto
            {
                GroupId = group.Id,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Message = "Hello"
            };

            // Act
            var result = await controller.CreateInvitation(dto, TestContext.Current.CancellationToken);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var value = statusCodeResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Erreur interne lors de l'envoi de l'invitation.", message);
        }
    }
}
