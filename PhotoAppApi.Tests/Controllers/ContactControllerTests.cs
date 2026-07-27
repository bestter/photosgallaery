using Microsoft.AspNetCore.Mvc;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Services;

namespace PhotoAppApi.Tests.Controllers
{
    public class ContactControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly ContactController _controller;

        public ContactControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _controller = new ContactController(_mockEmailService.Object);
        }

        [Fact]
        public async Task SubmitContactForm_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Hello",
                Message = "Test message"
            };

            // Act
            var result = await _controller.SubmitContactForm(request, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockEmailService.Verify(x => x.SendContactEmailAsync(request.Name, request.Email, request.Subject, request.Message, It.IsAny<CancellationToken>()), Times.Once);

            // Check anonymous object property
            var messageProp = okResult.Value.GetType().GetProperty("message");
            Assert.NotNull(messageProp);
            var messageValue = messageProp.GetValue(okResult.Value);
            Assert.Equal("Votre message a été envoyé avec succès.", messageValue);
        }

        [Theory]
        [InlineData("", "john@example.com", "Hello", "Test")]
        [InlineData("John", "", "Hello", "Test")]
        [InlineData("John", "john@example.com", "", "Test")]
        [InlineData("John", "john@example.com", "Hello", "")]
        [InlineData(null, "john@example.com", "Hello", "Test")]
        [InlineData("John", null, "Hello", "Test")]
        [InlineData("John", "john@example.com", null, "Test")]
        [InlineData("John", "john@example.com", "Hello", null)]
        [InlineData(" ", "john@example.com", "Hello", "Test")]
        [InlineData("John", " ", "Hello", "Test")]
        [InlineData("John", "john@example.com", " ", "Test")]
        [InlineData("John", "john@example.com", "Hello", " ")]
        public async Task SubmitContactForm_MissingFields_ReturnsBadRequest(string? name, string? email, string? subject, string? message)
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = name!,
                Email = email!,
                Subject = subject!,
                Message = message!
            };

            // Act
            var result = await _controller.SubmitContactForm(request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tous les champs sont requis.", badRequestResult.Value);
            _mockEmailService.Verify(x => x.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SubmitContactForm_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SubmitContactForm(null, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Requête invalide.", badRequestResult.Value);
            _mockEmailService.Verify(x => x.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SubmitContactForm_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "The Email field is not a valid e-mail address.");
            var request = new ContactRequestDto
            {
                Name = "John Doe",
                Email = "invalid-email",
                Subject = "Hello",
                Message = "Test message"
            };

            // Act
            var result = await _controller.SubmitContactForm(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _mockEmailService.Verify(x => x.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void SubmitContactForm_HasRequiredAttributes()
        {
            // Arrange
            var methodInfo = typeof(ContactController).GetMethod(nameof(ContactController.SubmitContactForm));

            // Act
            var hasHttpPost = methodInfo.GetCustomAttributes(typeof(HttpPostAttribute), false).Any();
            var hasRateLimiting = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute), false).Any();

            // Assert
            Assert.NotNull(methodInfo);
            Assert.True(hasHttpPost, "SubmitContactForm should have [HttpPost] attribute");
            Assert.True(hasRateLimiting, "SubmitContactForm should have [EnableRateLimiting] attribute");
        }

        [Fact]
        public async Task SubmitContactForm_ServiceThrowsException_Returns500AndLogs()
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Hello",
                Message = "Test message"
            };

            _mockEmailService
                .Setup(x => x.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test email exception"));

            // Act
            var result = await _controller.SubmitContactForm(request, TestContext.Current.CancellationToken);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("Une erreur s'est produite lors de l'envoi du message.", objectResult.Value);
        }
    }
}
