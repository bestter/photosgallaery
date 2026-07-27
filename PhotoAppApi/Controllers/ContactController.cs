using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoAppApi.Services;
using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ContactController));

        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ContactController(IEmailService emailService, IServiceScopeFactory serviceScopeFactory)
        {
            _emailService = emailService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpPost]
        [EnableRateLimiting("ContactLimiter")]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request == null)
            {
                return BadRequest("Requête invalide.");
            }

            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Subject) ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Tous les champs sont requis.");
            }

            try
            {
                // 🛡️ Sentinel: Fix User Enumeration / Timing vulnerability
                // Send the email in a fire-and-forget task to equalize response times
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        await emailService.SendContactEmailAsync(
                            request.Name,
                            request.Email,
                            request.Subject,
                            request.Message,
                            CancellationToken.None
                        );
                    }
                    catch (Exception ex)
                    {
                        log.Error("Erreur d'arrière-plan lors de l'envoi du formulaire de contact", ex);
                    }
                });

                return Ok(new { message = "Votre message a été envoyé avec succès." });
            }
            catch (Exception ex)
            {
                log.Error($"An error occurred in {nameof(SubmitContactForm)}", ex);
                // Ne pas exposer d'informations sensibles
                return StatusCode(500, "Une erreur s'est produite lors de l'envoi du message.");
            }
        }
    }

    public class ContactRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
