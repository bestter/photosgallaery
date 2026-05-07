using System.Text;
using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;        
        }

        public Task SendContactEmailAsync(string name, string email, string subject, string message)
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("[EMAIL SIMULATION] <h2>Nouveau message de contact via PixelLyra</h2>");
            _logger.LogInformation($"<p><strong>Nom :</strong> {name}</p>");
            _logger.LogInformation($"<p><strong>Courriel :</strong> {email}</p>");
            _logger.LogInformation($"<p><strong>Sujet :</strong> {subject}</p>");
            _logger.LogInformation($"<p><strong>Message :</strong><br/>{message.Replace("\n", "<br/>")}</p>");
            _logger.LogInformation("========================================");
            return Task.CompletedTask;
        }

        public Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl)
        {
            _logger.LogInformation  ("========================================");
            _logger.LogInformation($"[EMAIL SIMULATION] Sending invitation to {email}");
            _logger.LogInformation($"Subject: {inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision");
            _logger.LogInformation($"\nBonjour {firstName} {lastName},");
            _logger.LogInformation($"\nVous avez été invité par {inviterName} à rejoindre notre galerie privée.");
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                _logger.LogInformation($"\nMessage personnel : \"{message}\"");
            }
            
            _logger.LogInformation($"\nPour accepter l'invitation et créer votre compte, veuillez cliquer sur ce lien exclusif :");
            _logger.LogInformation($"URL : {inviteUrl}");
            _logger.LogInformation("========================================");

            return Task.CompletedTask;
        }
    }
}
