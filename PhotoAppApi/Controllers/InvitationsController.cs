using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private static readonly ILog log = LogManager.GetLogger(typeof(InvitationsController));

        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;


        public InvitationsController(AppDbContext context, IEmailService emailService, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
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

                // 🛡️ Sentinel: Fix User Enumeration vulnerability
                // Standardize the response message to avoid leaking user existence or group membership status.
                var genericSuccessMessage = "Si l'adresse e-mail est valide, une invitation a été envoyée ou l'utilisateur a été ajouté au groupe.";

                var frontendUrl = _configuration.GetValue<string>("FrontendUrl"); // On pourrait l'injecter depuis appsettings

                var groupId = group.Id;
                var groupName = group.Name;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, CancellationToken.None);
                        if (existingUser != null)
                        {
                            // L'utilisateur a déjà un compte. Vérifions s'il est déjà dans le groupe.
                            bool alreadyInGroup = await dbContext.UserGroups.AnyAsync(ug => ug.UserId == existingUser.Id && ug.GroupId == groupId, CancellationToken.None);
                            if (!alreadyInGroup)
                            {
                                // L'ajouter directement au groupe sans envoyer d'invitation avec token
                                dbContext.UserGroups.Add(new UserGroup { UserId = existingUser.Id, GroupId = groupId });
                                await dbContext.SaveChangesAsync(CancellationToken.None);
                            }
                        }
                        else
                        {
                            // Vérifier s'il a déjà été invité
                            var existingInvite = await dbContext.GroupInvitations
                                .FirstOrDefaultAsync(i => i.GroupId == groupId && i.Email == dto.Email && i.Status == "Pending", CancellationToken.None);

                            if (existingInvite == null)
                            {
                                var invitation = new GroupInvitation
                                {
                                    GroupId = groupId,
                                    InviterId = inviterId,
                                    FirstName = dto.FirstName,
                                    LastName = dto.LastName,
                                    Email = dto.Email,
                                    Message = dto.Message
                                };

                                dbContext.GroupInvitations.Add(invitation);
                                await dbContext.SaveChangesAsync(CancellationToken.None);

                                var inviteUrl = $"{frontendUrl}/join/{invitation.InviteToken}";

                                await emailService.SendInvitationEmailAsync(
                                    email: invitation.Email,
                                    firstName: invitation.FirstName,
                                    lastName: invitation.LastName,
                                    inviterName: inviterUsername,
                                    groupName: groupName,
                                    message: invitation.Message ?? "",
                                    inviteUrl: inviteUrl,
                                    CancellationToken.None
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Erreur d'arrière-plan lors de la creation et de l'envoi de l'invitation", ex);
                    }
                });

                return Ok(new { message = genericSuccessMessage });
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
