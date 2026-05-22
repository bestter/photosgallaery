using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System.ComponentModel.DataAnnotations;
using log4net;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationsController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(InvitationsController));

                private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;


        public InvitationsController(AppDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost]
        [Authorize]
        [EnableRateLimiting("InviteLimiter")]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto, CancellationToken cancellationToken = default)
        {
            log.Debug($"In {nameof(CreateInvitation)} for email: {dto.Email} in group: {dto.GroupId}");
            try
            {
                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId and Username directly from JWT claims.
                var inviterIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var inviterUsername = User.Identity?.Name;
                if (!int.TryParse(inviterIdString, out int inviterId) || string.IsNullOrEmpty(inviterUsername)) return Unauthorized();

                // Validation : L'inviteur fait-il partie du groupe ?
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == dto.GroupId, cancellationToken);
                if (group == null) return NotFound(new { message = "Cercle introuvable." });

                bool isMember = await _context.UserGroups.AnyAsync(ug => ug.UserId == inviterId && ug.GroupId == group.Id, cancellationToken);
                if (!isMember && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // --- AJOUT : Vérifier si l'utilisateur existe déjà
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
                if (existingUser != null)
                {
                    // L'utilisateur a déjà un compte. Vérifions s'il est déjà dans le groupe.
                    bool alreadyInGroup = await _context.UserGroups.AnyAsync(ug => ug.UserId == existingUser.Id && ug.GroupId == group.Id, cancellationToken);
                    if (alreadyInGroup)
                    {
                        return BadRequest(new { message = "Cet utilisateur fait déjà partie du cercle." });
                    }
                    else
                    {
                        // L'ajouter directement au groupe sans envoyer d'invitation avec token
                        _context.UserGroups.Add(new UserGroup { UserId = existingUser.Id, GroupId = group.Id });
                        await _context.SaveChangesAsync(cancellationToken);
                        return Ok(new { message = $"L'utilisateur existant a été ajouté automatiquement au cercle !" });
                    }
                }
                // --- FIN AJOUT

                // Vérifier s'il a déjà été invité
                var existingInvite = await _context.GroupInvitations
                    .FirstOrDefaultAsync(i => i.GroupId == dto.GroupId && i.Email == dto.Email && i.Status == "Pending", cancellationToken);

                if (existingInvite != null)
                {
                    return BadRequest(new { message = "Une invitation est déjà en attente pour cette adresse e-mail dans ce cercle." });
                }

                var invitation = new GroupInvitation
                {
                    GroupId = group.Id,
                    InviterId = inviterId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Message = dto.Message
                };

                _context.GroupInvitations.Add(invitation);
                await _context.SaveChangesAsync(cancellationToken);

                // L'URL pointe vers le Frontend (React) avec le token généré
                var frontendUrl = _configuration.GetValue<string>("FrontendUrl"); // On pourrait l'injecter depuis appsettings
                var inviteUrl = $"{frontendUrl}/join/{invitation.InviteToken}";

                await _emailService.SendInvitationEmailAsync(
                    email: invitation.Email,
                    firstName: invitation.FirstName,
                    lastName: invitation.LastName,
                    inviterName: inviterUsername,
                    groupName: group.Name,
                    message: invitation.Message ?? "",
                    inviteUrl: inviteUrl, 
                    cancellationToken
                );

                return Ok(new { message = $"Invitation envoyée avec succès à {invitation.Email} !" });
            }
            catch (Exception ex)
            {
                log.Error("Erreur lors de la création de l'invitation", ex);
                return StatusCode(500, new { message = "Erreur interne lors de l'envoi de l'invitation." });
            }
        }
    }

    public class CreateInvitationDto
    {
        [Required(ErrorMessage = "L'ID du cercle est requis.")]
        public Guid GroupId { get; set; }

        [Required(ErrorMessage = "Le prénom est requis.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Message { get; set; }
    }
}
