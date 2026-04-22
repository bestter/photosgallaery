using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Logger _logger;

        public GroupRequestsController(AppDbContext context)
        {
            _context = context;
            _logger = new Logger();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllGroupRequests()
        {
            _logger.Debug($"In {nameof(GetAllGroupRequests)}");
            try
            {
                var requests = await _context.GroupRequests
                    .Include(r => r.Requester)
                    .OrderByDescending(r => r.RequestedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description,
                        r.RequestedAt,
                        Requester = new
                        {
                            r.Requester.Id,
                            r.Requester.Username,
                            r.Requester.Email
                        }
                    })
                    .ToListAsync();
                
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(GetAllGroupRequests)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la récupération des demandes." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGroupRequest(Guid id)
        {
            _logger.Debug($"In {nameof(DeleteGroupRequest)} with id: {id}");
            try
            {
                var request = await _context.GroupRequests.FindAsync(id);
                if (request == null)
                {
                    return NotFound(new { message = "Demande non trouvée." });
                }

                _context.GroupRequests.Remove(request);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande supprimée avec succès." });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(DeleteGroupRequest)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la suppression de la demande." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitGroupRequest([FromBody] SubmitGroupRequestDto request)
        {
            _logger.Debug($"In {nameof(SubmitGroupRequest)} for group: {request.Name}");
            try
            {
                var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (idClaim == null || !int.TryParse(idClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "Utilisateur non trouvé dans le token." });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "Utilisateur non trouvé." });
                }

                var groupRequest = new GroupRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    Requester = user,
                    RequestedAt = DateTime.UtcNow
                };

                _context.GroupRequests.Add(groupRequest);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Demande de création de groupe envoyée avec succès.", id = groupRequest.Id });
            }
            catch (Exception ex)
            {
                _logger.Error($"An error occured in {nameof(SubmitGroupRequest)}", ex);
                return StatusCode(500, new { message = "Erreur lors de la création de la demande de groupe." });
            }
        }
    }

    public class SubmitGroupRequestDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
