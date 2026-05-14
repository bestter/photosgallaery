using log4net;
using System.Text;
using System.Threading.Tasks;
using System.Net;


namespace PhotoAppApi.Services
{
    public class MockEmailService : IEmailService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MockEmailService));

                public MockEmailService()
        {
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

        public Task SendContactEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        {
            var sanitizedName = SanitizeForLog(name);
            var sanitizedEmail = SanitizeForLog(email);
            var sanitizedSubject = SanitizeForLog(subject);
            var sanitizedMessage = WebUtility.HtmlEncode(message ?? string.Empty)
                .Replace("\r\n", "<br/>")
                .Replace("\n", "<br/>")
                .Replace("\r", "<br/>");

            log.Info("========================================");
            log.Info("[EMAIL SIMULATION] <h2>Nouveau message de contact via PixelLyra</h2>");
            log.Info("<p><strong>Nom :</strong> {sanitizedName}</p>");
            log.Info("<p><strong>Courriel :</strong> {sanitizedEmail}</p>");
            log.Info("<p><strong>Sujet :</strong> {sanitizedSubject}</p>");
            log.Info("<p><strong>Message :</strong><br/>{sanitizedMessage}</p>");
            log.Info("========================================");
            return Task.CompletedTask;
        }

        public Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl, CancellationToken cancellationToken = default)
        {
            var sanitizedEmail = SanitizeForLog(email);
            var sanitizedFirstName = SanitizeForLog(firstName);
            var sanitizedLastName = SanitizeForLog(lastName);
            var sanitizedInviterName = SanitizeForLog(inviterName);
            var sanitizedGroupName = SanitizeForLog(groupName);
            var sanitizedMessage = SanitizeForLog(message);
            var sanitizedInviteUrl = SanitizeForLog(inviteUrl);

            log.Info  ("========================================");
            log.Info("[EMAIL SIMULATION] Sending invitation to {sanitizedEmail}");
            log.Info("Subject: {sanitizedInviterName} vous a invité à rejoindre le cercle {sanitizedGroupName} sur Vision");
            log.Info("\nBonjour {sanitizedFirstName} {sanitizedLastName},");
            log.Info("\nVous avez été invité par {sanitizedInviterName} à rejoindre notre galerie privée.");

            if (!string.IsNullOrWhiteSpace(message))
            {
                log.Info($"\nMessage personnel : \"{sanitizedMessage}\"");
            }

            log.Info("\nPour accepter l'invitation et créer votre compte, veuillez cliquer sur ce lien exclusif :");
            log.Info("URL : {sanitizedInviteUrl}");
            log.Info("========================================");

            return Task.CompletedTask;
        }
    }
}
