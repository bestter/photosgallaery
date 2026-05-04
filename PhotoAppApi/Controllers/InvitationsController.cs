using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvitationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly Logger _logger = new();

        public InvitationsController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationDto dto)
        {
            _logger.Debug($"In {nameof(CreateInvitation)} for email: {dto.Email} in group: {dto.GroupId}");
            try
            {
                // ⚡ Bolt: Eliminate redundant Users table query by extracting UserId and Username directly from JWT claims.
                var inviterIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var inviterUsername = User.Identity?.Name;
                if (!int.TryParse(inviterIdString, out int inviterId) || string.IsNullOrEmpty(inviterUsername)) return Unauthorized();

                // Validation : L'inviteur fait-il partie du groupe ?
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == dto.GroupId);
                if (group == null) return NotFound(new { message = "Cercle introuvable." });

                bool isMember = await _context.UserGroups.AnyAsync(ug => ug.UserId == inviterId && ug.GroupId == group.Id);
                if (!isMember && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // --- AJOUT : Vérifier si l'utilisateur existe déjà
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    // L'utilisateur a déjà un compte. Vérifions s'il est déjà dans le groupe.
                    bool alreadyInGroup = await _context.UserGroups.AnyAsync(ug => ug.UserId == existingUser.Id && ug.GroupId == group.Id);
                    if (alreadyInGroup)
                    {
                        return BadRequest(new { message = "Cet utilisateur fait déjà partie du cercle." });
                    }
                    else
                    {
                        // L'ajouter directement au groupe sans envoyer d'invitation avec token
                        _context.UserGroups.Add(new UserGroup { UserId = existingUser.Id, GroupId = group.Id });
                        await _context.SaveChangesAsync();
                        return Ok(new { message = $"L'utilisateur existant a été ajouté automatiquement au cercle !" });
                    }
                }
                // --- FIN AJOUT

                // Vérifier s'il a déjà été invité
                var existingInvite = await _context.GroupInvitations
                    .FirstOrDefaultAsync(i => i.GroupId == dto.GroupId && i.Email == dto.Email && i.Status == "Pending");
                
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
                await _context.SaveChangesAsync();

                // L'URL pointe vers le Frontend (React) avec le token généré
                var frontendUrl = "http://localhost:5173"; // On pourrait l'injecter depuis appsettings
                var inviteUrl = $"{frontendUrl}/join/{invitation.InviteToken}";

                await _emailService.SendInvitationEmailAsync(
                    email: invitation.Email,
                    firstName: invitation.FirstName,
                    lastName: invitation.LastName,
                    inviterName: inviterUsername,
                    groupName: group.Name,
                    message: invitation.Message ?? "",
                    inviteUrl: inviteUrl
                );

                return Ok(new { message = $"Invitation envoyée avec succès à {invitation.Email} !" });
            }
            catch (Exception ex)
            {
                _logger.Error("Erreur lors de la création de l'invitation", ex);
                return StatusCode(500, new { message = "Erreur interne lors de l'envoi de l'invitation." });
            }
        }
    }

    public class CreateInvitationDto
    {
        [Required(ErrorMessage = "L'ID du cercle est requis.")]
        public Guid GroupId { get; set; }

        [Required(ErrorMessage = "Le prénom est requis.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse e-mail est requise.")]
        [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
        public string Email { get; set; } = string.Empty;

        public string? Message { get; set; }
    }
}
