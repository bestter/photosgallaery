using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resend;
using System.Text;

namespace PhotoAppApi.Services
{
    public class ResendEmailService: IEmailService
    {

        private string toEmail;
        public ResendEmailService(IConfiguration configuration)
        {
            var _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            ResendKEy = _configuration["Resend:Key"] ?? throw new ArgumentNullException("Resend:Key");
            //fromEmail = _configuration["Resend:FromEmail"] ?? throw new ArgumentNullException("Resend:FromEmail");
            toEmail = _configuration["Resend:ToEmail"] ?? throw new ArgumentNullException("Resend:ToEmail");
        }

        private string ResendKEy { get; set; }

        public async Task SendInvitationEmailAsync(string email, string firstName, string lastName, string inviterName, string groupName, string message, string inviteUrl, CancellationToken cancellationToken = default)
        {
            StringBuilder sb = new();
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

            await EnvoyerCourrielAsync(email, $"{inviterName} vous a invité à rejoindre le cercle {groupName} sur Vision", sb.ToString(), cancellationToken);
        }

        public async Task SendContactEmailAsync(string name, string email, string subject, string message, CancellationToken cancellationToken = default)
        {
            StringBuilder sb = new();
            sb.AppendLine("<h2>Nouveau message de contact via PixelLyra</h2>");
            sb.AppendLine($"<p><strong>Nom :</strong> {name}</p>");
            sb.AppendLine($"<p><strong>Courriel :</strong> {email}</p>");
            sb.AppendLine($"<p><strong>Sujet :</strong> {subject}</p>");
            sb.AppendLine($"<p><strong>Message :</strong><br/>{message.Replace("\n", "<br/>")}</p>");

            await EnvoyerCourrielAsync(email, $"Contact - {subject}", sb.ToString(), cancellationToken);
        }

        private async Task EnvoyerCourrielAsync(string from, string sujet, string contenuHtml, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ResendKEy))
            {
                throw new NotSupportedException("La clé API pour Resend n'est pas définie !");
            }
            ArgumentException.ThrowIfNullOrWhiteSpace(from);
            ArgumentException.ThrowIfNullOrWhiteSpace(sujet);
            ArgumentException.ThrowIfNullOrWhiteSpace(contenuHtml);
            try
            {
                if (string.Equals(toEmail, from, StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException("L'adresse e-mail de destination ne peut pas être la même que l'adresse e-mail d'expédition.");
                }
                IResend resend = ResendClient.Create(ResendKEy);

                var resp = await resend.EmailSendAsync(new EmailMessage()
                {
                    From = from,
                    To = toEmail,
                    Subject = sujet,
                    HtmlBody = contenuHtml,
                }, cancellationToken);

            }
            catch (Exception ex)
            {
                throw new NotSupportedException("An error occurred while sending the email.", ex);
            }
        }
    }
}
