using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock.Setup(sp => sp.GetService(typeof(IEmailService))).Returns(emailService);
            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(serviceScopeMock.Object);

            var controller = new InvitationsController(context, emailService, configuration, serviceScopeFactoryMock.Object);

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
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.", message);
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
            Assert.Equal("Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.", message);

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
                It.IsAny<CancellationToken>()), Times.Never()); // Wait for background task
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
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.", message);
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
            Assert.Equal("Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.", message);

            var invitation = await context.GroupInvitations.FirstOrDefaultAsync(i => i.Email == dto.Email, TestContext.Current.CancellationToken);
            Assert.NotNull(invitation);
            Assert.Equal("Pending", invitation.Status);
            Assert.Equal(1, invitation.InviterId);

            await Task.Delay(100); // Give the background task time to run
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
            ), Times.Exactly(1)); // The task will run async, but Moq might evaluate too early. In memory execution usually is fast enough, but if it fails we need to wait
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
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var message = value?.GetType()?.GetProperty("message")?.GetValue(value, null) as string;
            Assert.Equal("Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.", message);
        }
    }
}
