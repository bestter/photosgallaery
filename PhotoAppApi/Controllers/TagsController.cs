using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;

namespace PhotoAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tags/search?q=nat
        [HttpGet("search")]
        public async Task<IActionResult> SearchTags([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<string>());

            var query = q.ToLower();

            // On cherche dans les noms de tags en français
            var suggestions = await _context.TagTranslations
                .Where(tt => tt.Language == Language.FR && tt.Name.ToLower().Contains(query))
                .Select(tt => tt.Name)
                .Distinct()
                .Take(10) // On limite à 10 pour la performance
                .ToListAsync();

            return Ok(suggestions);
        }
    }
}