using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class MockEmailServiceTests
    {
        private readonly TestMemoryAppender _appender;
        private readonly MockEmailService _service;

        public MockEmailServiceTests()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders();

            _appender = new TestMemoryAppender();
            BasicConfigurator.Configure(hierarchy, _appender);

            _service = new MockEmailService();
        }

        [Fact]
        public async Task SendContactEmailAsync_LogsOutput()
        {
            // Arrange
            string name = "John Doe";
            string email = "test@example.com";
            string subject = "Hello World";
            string message = "This is a message.";

            // Act
            await _service.SendContactEmailAsync(name, email, subject, message, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();
            Assert.Contains(events, e => e.Contains("[EMAIL SIMULATION] <h2>Nouveau message de contact via PixelLyra</h2>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Nom :</strong> {name}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Courriel :</strong> {email}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Sujet :</strong> {subject}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Message :</strong><br/>{message}</p>"));
        }

        [Fact]
        public async Task SendContactEmailAsync_SanitizesOutput()
        {
            // Arrange
            string name = "John \n Doe <script>alert('xss')</script>";
            string email = "test@example.com\r\n";
            string subject = "Hello \r World";
            string message = "This is a \n multi-line <script> message.";

            // Act
            await _service.SendContactEmailAsync(name, email, subject, message, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();

            var expectedName = "John  Doe &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;";
            var expectedEmail = "test@example.com";
            var expectedSubject = "Hello  World";
            var expectedMessage = "This is a <br/> multi-line &lt;script&gt; message.";

            Assert.Contains(events, e => e.Contains($"<p><strong>Nom :</strong> {expectedName}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Courriel :</strong> {expectedEmail}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Sujet :</strong> {expectedSubject}</p>"));
            Assert.Contains(events, e => e.Contains($"<p><strong>Message :</strong><br/>{expectedMessage}</p>"));
        }

        [Fact]
        public async Task SendInvitationEmailAsync_LogsOutput()
        {
            // Arrange
            string email = "invite@example.com";
            string firstName = "Jane";
            string lastName = "Doe";
            string inviterName = "John";
            string groupName = "Family";
            string message = "Welcome!";
            string inviteUrl = "http://example.com/invite";

            // Act
            await _service.SendInvitationEmailAsync(email, firstName, lastName, inviterName, groupName, message, inviteUrl, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();
            Assert.Contains(events, e => e.Contains($"[EMAIL SIMULATION] Sending invitation to {email}"));
            Assert.Contains(events, e => e.Contains($"Subject: {inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision"));
            Assert.Contains(events, e => e.Contains($"Message personnel : \"{message}\""));
            Assert.Contains(events, e => e.Contains($"URL : {inviteUrl}"));
            Assert.Contains(events, e => e.Contains($"\nBonjour {firstName} {lastName},"));
            Assert.Contains(events, e => e.Contains($"\nVous avez été invité par {inviterName} à rejoindre notre galerie privée."));
        }

        [Fact]
        public async Task SendInvitationEmailAsync_SanitizesOutput()
        {
            // Arrange
            string email = "invite\n@example.com";
            string firstName = "Jane <script>";
            string lastName = "Doe \r";
            string inviterName = "John \n";
            string groupName = "Family <b>";
            string message = "Welcome! \r\n";
            string inviteUrl = "http://example.com/invite?test=1&x=2";

            // Act
            await _service.SendInvitationEmailAsync(email, firstName, lastName, inviterName, groupName, message, inviteUrl, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();

            var expectedEmail = "invite@example.com";
            var expectedFirstName = "Jane &lt;script&gt;";
            var expectedLastName = "Doe ";
            var expectedInviterName = "John ";
            var expectedGroupName = "Family &lt;b&gt;";
            var expectedMessage = "Welcome! ";
            var expectedInviteUrl = "http://example.com/invite?test=1&amp;x=2";

            Assert.Contains(events, e => e.Contains($"[EMAIL SIMULATION] Sending invitation to {expectedEmail}"));
            Assert.Contains(events, e => e.Contains($"Subject: {expectedInviterName} vous a invité à rejoindre le cercle {expectedGroupName} sur Vision"));
            Assert.Contains(events, e => e.Contains($"Message personnel : \"{expectedMessage}\""));
            Assert.Contains(events, e => e.Contains($"URL : {expectedInviteUrl}"));
        }

        [Fact]
        public async Task SendInvitationEmailAsync_NullMessage_DoesNotLogMessageLine()
        {
            // Arrange
            string email = "invite@example.com";
            string firstName = "Jane";
            string lastName = "Doe";
            string inviterName = "John";
            string groupName = "Family";
            string message = null;
            string inviteUrl = "http://example.com/invite";

            // Act
            await _service.SendInvitationEmailAsync(email, firstName, lastName, inviterName, groupName, message, inviteUrl, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();
            Assert.DoesNotContain(events, e => e.Contains("Message personnel"));
        }

        [Fact]
        public async Task SendContactEmailAsync_NullMessage_HandlesNullGracefully()
        {
            // Arrange
            string name = "John Doe";
            string email = "test@example.com";
            string subject = "Hello World";
            string message = null;

            // Act
            await _service.SendContactEmailAsync(name, email, subject, message, CancellationToken.None);

            // Assert
            var events = _appender.GetEvents().Select(e => e.RenderedMessage).ToList();
            Assert.Contains(events, e => e.Contains($"<p><strong>Message :</strong><br/></p>"));
        }
    }
}
