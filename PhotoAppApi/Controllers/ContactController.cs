using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoAppApi.Services;
using log4net;

namespace PhotoAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ContactController));

                private readonly IEmailService _emailService;
        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;

        }

        [HttpPost]
        [EnableRateLimiting("ContactLimiter")]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactRequestDto request)
        {
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
                await _emailService.SendContactEmailAsync(request.Name, request.Email, request.Subject, request.Message);
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
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}
