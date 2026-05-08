using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PhotoAppApi.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        private static string SanitizeForLog(string input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            var noLineBreaks = input.Replace("\r", "").Replace("\n", "");
            return WebUtility.HtmlEncode(noLineBreaks);
        }

        public Task SendContactEmailAsync(string name, string email, string subject, string message)
        {
            var sanitizedName = SanitizeForLog(name);
            var sanitizedEmail = SanitizeForLog(email);
            var sanitizedSubject = SanitizeForLog(subject);
            var sanitizedMessage = WebUtility.HtmlEncode(message ?? string.Empty)
                .Replace("\r\n", "<br/>")
                .Replace("\n", "<br/>")
                .Replace("\r", "<br/>");

            _logger.LogInformation("========================================");
            _logger.LogInformation("[EMAIL SIMULATION] <h2>Nouveau message de contact via PixelLyra</h2>");
            _logger.LogInformation($"<p><strong>Nom :</strong> {sanitizedName}</p>");
            _logger.LogInformation($"<p><strong>Courriel :</strong> {sanitizedEmail}</p>");
            _logger.LogInformation($"<p><strong>Sujet :</strong> {sanitizedSubject}</p>");
            _logger.LogInformation($"<p><strong>Message :</strong><br/>{sanitizedMessage}</p>");
            _logger.LogInformation("========================================");
            return Task.CompletedTask;
        }

        public Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl)
        {
            var sanitizedEmail = SanitizeForLog(email);
            var sanitizedFirstName = SanitizeForLog(firstName);
            var sanitizedLastName = SanitizeForLog(lastName);
            var sanitizedInviterName = SanitizeForLog(inviterName);
            var sanitizedGroupName = SanitizeForLog(groupName);
            var sanitizedMessage = SanitizeForLog(message);
            var sanitizedInviteUrl = SanitizeForLog(inviteUrl);

            _logger.LogInformation("========================================");
            _logger.LogInformation($"[EMAIL SIMULATION] Sending invitation to {sanitizedEmail}");
            _logger.LogInformation($"Subject: {sanitizedInviterName} vous a invité à rejoindre le cercle {sanitizedGroupName} sur Vision");
            _logger.LogInformation($"\nBonjour {sanitizedFirstName} {sanitizedLastName},");
            _logger.LogInformation($"\nVous avez été invité par {sanitizedInviterName} à rejoindre notre galerie privée.");

            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation($"\nMessage personnel : \"{sanitizedMessage}\"");
            }

            _logger.LogInformation($"\nPour accepter l'invitation et créer votre compte, veuillez cliquer sur ce lien exclusif :");
            _logger.LogInformation($"URL : {sanitizedInviteUrl}");
            _logger.LogInformation("========================================");

            return Task.CompletedTask;
        }
    }
}
