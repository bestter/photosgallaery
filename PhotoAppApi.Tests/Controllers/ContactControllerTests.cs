using Microsoft.AspNetCore.Mvc;
using Moq;
using PhotoAppApi.Controllers;
using PhotoAppApi.Services;
using System;
using System.Threading.Tasks;
using Xunit;

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
        public async Task SubmitContactForm_ValidRequest_ReturnsOkAndCallsEmailService()
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Hello",
                Message = "This is a test message."
            };

            // Act
            var result = await _controller.SubmitContactForm(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);

            // Verify email service was called
            _mockEmailService.Verify(
                s => s.SendContactEmailAsync(request.Name, request.Email, request.Subject, request.Message),
                Times.Once);
        }

        [Theory]
        [InlineData("", "john@example.com", "Hello", "Message")]
        [InlineData("John", "", "Hello", "Message")]
        [InlineData("John", "john@example.com", "", "Message")]
        [InlineData("John", "john@example.com", "Hello", "")]
        [InlineData(null, "john@example.com", "Hello", "Message")]
        [InlineData("John", null, "Hello", "Message")]
        [InlineData("John", "john@example.com", null, "Message")]
        [InlineData("John", "john@example.com", "Hello", null)]
        public async Task SubmitContactForm_MissingFields_ReturnsBadRequest(string name, string email, string subject, string message)
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = name,
                Email = email,
                Subject = subject,
                Message = message
            };

            // Act
            var result = await _controller.SubmitContactForm(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Tous les champs sont requis.", badRequestResult.Value);

            // Verify email service was NOT called
            _mockEmailService.Verify(
                s => s.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SubmitContactForm_EmailServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new ContactRequestDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                Subject = "Hello",
                Message = "This is a test message."
            };

            _mockEmailService
                .Setup(s => s.SendContactEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP Error"));

            // Act
            var result = await _controller.SubmitContactForm(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Une erreur s'est produite lors de l'envoi du message.", statusCodeResult.Value);
        }
    }
}
