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

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/groups
        [HttpGet]
        public async Task<IActionResult> GetAllGroups()
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

        // POST: api/admin/groups
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
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

        // GET: api/admin/groups/{id}/members
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetGroupMembers(Guid id)
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

        // POST: api/admin/groups/{id}/members
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
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

        // DELETE: api/admin/groups/{id}/members/{userId}
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(Guid id, int userId)
        {
            var userGroup = await _context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == id && ug.UserId == userId);
            if (userGroup == null) return NotFound(new { message = "Membre non trouvé dans ce groupe." });

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Membre retiré du groupe." });
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
