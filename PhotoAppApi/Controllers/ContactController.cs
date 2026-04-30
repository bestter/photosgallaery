using Microsoft.AspNetCore.Mvc;
using PhotoAppApi.Services;

namespace PhotoAppApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitContactForm([FromBody] ContactRequestDto request)
        {
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
            catch (Exception)
            {
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
