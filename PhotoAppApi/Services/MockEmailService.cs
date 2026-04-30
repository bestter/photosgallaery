using System.Text;
using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly Logger _logger = new();

        public MockEmailService()
        {
        }

        public Task SendContactEmailAsync(string name, string email, string subject, string message)
        {
            _logger.Info("========================================");
            _logger.Info("[EMAIL SIMULATION] <h2>Nouveau message de contact via PixelLyra</h2>");
            _logger.Info($"<p><strong>Nom :</strong> {name}</p>");
            _logger.Info($"<p><strong>Courriel :</strong> {email}</p>");
            _logger.Info($"<p><strong>Sujet :</strong> {subject}</p>");
            _logger.Info($"<p><strong>Message :</strong><br/>{message.Replace("\n", "<br/>")}</p>");
            _logger.Info("========================================");

            return Task.CompletedTask;
        }

        public Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl)
        {
            _logger.Info("========================================");
            _logger.Info($"[EMAIL SIMULATION] Sending invitation to {email}");
            _logger.Info($"Subject: {inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision");
            _logger.Info($"\nBonjour {firstName} {lastName},");
            _logger.Info($"\nVous avez été invité par {inviterName} à rejoindre notre galerie privée.");
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.Info($"\nMessage personnel : \"{message}\"");
            }
            
            _logger.Info($"\nPour accepter l'invitation et créer votre compte, veuillez cliquer sur ce lien exclusif :");
            _logger.Info($"URL : {inviteUrl}");
            _logger.Info("========================================");

            return Task.CompletedTask;
        }
    }
}
