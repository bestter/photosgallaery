using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using System;
using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public class GroupService
    {
        private readonly AppDbContext _context;

        public GroupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueSlugAsync(string groupName, CancellationToken cancellationToken = default)
        {
            // On génère le slug de base
            string baseSlug = SlugGenerator.GenerateSlug(groupName);

            // S'il est vide après nettoyage, on génère un fallback
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = "group";
            }

            string uniqueSlug = baseSlug;

            // On récupère tous les slugs existants qui commencent par la base pour éviter les requêtes N+1
            var existingSlugsList = await _context.Groups
                .AsNoTracking()
                .Where(g => g.ShortName != null && g.ShortName.StartsWith(baseSlug))
                .Select(g => g.ShortName)
                .ToListAsync(cancellationToken);

            var existingSlugs = existingSlugsList.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // On vérifie dans le HashSet si le slug existe déjà
            while (existingSlugs.Contains(uniqueSlug))
            {
                // S'il y a une collision, on ajoute un petit ID de 5 caractères
                string shortId = Guid.NewGuid().ToString("N").Substring(0, 5);
                uniqueSlug = $"{baseSlug}-{shortId}";
            }

            return uniqueSlug;
        }
    }
}
