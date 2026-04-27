using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resend;
using System.Text;

namespace PhotoAppApi.Services
{
    public class ResendEmailService: IEmailService
    {

        private string fromEmail;
        public ResendEmailService(IConfiguration configuration)
        {
            var _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ResendKEy = _configuration["Resend:Key"] ?? throw new ArgumentNullException("Resend:Key");
            fromEmail = _configuration["Resend:FromEmail"] ?? throw new ArgumentNullException("Resend:FromEmail");
        }   

        private string ResendKEy { get; set; }

        public async Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========================================");            
            sb.AppendLine($"Subject: {inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision");
            sb.AppendLine($"\nBonjour {firstName} {lastName},");
            sb.AppendLine($"\nVous avez été invité par {inviterName} à rejoindre notre galerie privée.");

            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.AppendLine($"\nMessage personnel : \"{message}\"");
            }

            sb.AppendLine($"\nPour accepter l'invitation et créer votre compte, veuillez cliquer sur ce lien exclusif :");
            sb.AppendLine($"URL : {inviteUrl}");
            sb.AppendLine("========================================");

            await EnvoyerCourrielAsync(email, $"{inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision", sb.ToString());
        }

        private async Task EnvoyerCourrielAsync(string destinataire, string sujet, string contenuHtml)
        {
            if (string.IsNullOrWhiteSpace(ResendKEy))
            {
                throw new NotSupportedException("La clé API pour Resend n'est pas définie !");
            }
            ArgumentException.ThrowIfNullOrWhiteSpace(destinataire);
            ArgumentException.ThrowIfNullOrWhiteSpace(sujet);
            ArgumentException.ThrowIfNullOrWhiteSpace(contenuHtml);
            try
            {               

                IResend resend = ResendClient.Create(ResendKEy);

                var resp = await resend.EmailSendAsync(new EmailMessage()
                {
                    From = fromEmail,
                    To = destinataire,
                    Subject = sujet,
                    HtmlBody = contenuHtml,
                });

            }
            catch (Exception ex)
            {
                throw new NotSupportedException("An error occurred while sending the email.", ex);
            }
        }
    }
}
