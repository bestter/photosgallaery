using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using System.ComponentModel.DataAnnotations;

namespace PhotoAppApi.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class GroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Logger _logger;

        public GroupsController(AppDbContext context)
        {
            _context = context;
            _logger = new();
        }

        // GET: api/admin/groups
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
        {
            _logger.Debug($"In {nameof(GetAllGroups)}");
            try
            {
                var groups = await _context.Groups
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.InviteToken,
                        g.CreatedAt,
                        UserCount = g.UserGroups.Count(),
                        PhotoCount = g.Photos.Count()
                    })
                    .ToListAsync();

                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetAllGroups)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des groupes." });
            }
        }

        // POST: api/admin/groups
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            _logger.Debug($"In {nameof(CreateGroup)} with name: {request.Name}");
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Le nom du groupe est requis." });
                }

                var group = new Group
                {
                    Name = request.Name
                };

                await _context.Groups.AddAsync(group);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    group.Id,
                    group.Name,
                    group.InviteToken,
                    group.CreatedAt,
                    UserCount = 0,
                    PhotoCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(CreateGroup)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la création du groupe." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            _logger.Debug($"In {nameof(DeleteGroup)} with id: {id}");
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    return NotFound(new { message = "Groupe non trouvé." });
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Groupe supprimé avec succès." });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(DeleteGroup)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la suppression du groupe." });
            }
        }

        // GET: api/admin/groups/{id}/members
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetGroupMembers(Guid id)
        {
            _logger.Debug($"In {nameof(GetGroupMembers)} with id: {id}");
            try
            {
                var members = await _context.UserGroups
                    .Where(ug => ug.GroupId == id)
                    .Select(ug => new
                    {
                        ug.UserId,
                        ug.User.Username,
                        ug.User.Email,
                        ug.JoinedAt
                    })
                    .ToListAsync();

                return Ok(members);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetGroupMembers)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des membres." });
            }
        }

        // POST: api/admin/groups/{id}/members
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
        {
            _logger.Debug($"In {nameof(AddMember)} with groupId: {id}, userId: {request.UserId}");
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
                if (!userExists) return NotFound(new { message = "Utilisateur non trouvé." });

                var groupExists = await _context.Groups.AnyAsync(g => g.Id == id);
                if (!groupExists) return NotFound(new { message = "Groupe non trouvé." });

                var alreadyInGroup = await _context.UserGroups.AnyAsync(ug => ug.GroupId == id && ug.UserId == request.UserId);
                if (alreadyInGroup) return BadRequest(new { message = "Cet utilisateur est déjà dans le groupe." });

                var userGroup = new UserGroup
                {
                    GroupId = id,
                    UserId = request.UserId
                };

                await _context.UserGroups.AddAsync(userGroup);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Membre ajouté avec succès." });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(AddMember)}", ex);
                return StatusCode(500, new { message = "Erreur lors de l'ajout du membre." });
            }
        }

        // DELETE: api/admin/groups/{id}/members/{userId}
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid id, int userId)
        {
            _logger.Debug($"In {nameof(RemoveMember)} with groupId: {id}, userId: {userId}");
            try
            {
                var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == id && ug.UserId == userId);
                if (userGroup == null) return NotFound(new { message = "Membre non trouvé dans ce groupe." });

                _context.UserGroups.Remove(userGroup);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Membre retiré du groupe." });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(RemoveMember)}", ex);
                return StatusCode(500, new { message = "Erreur lors du retrait du membre." });
            }
        }
    }

    public class CreateGroupRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    public class AddMemberRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}
