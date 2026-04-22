using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using PhotoAppApi.Data;

namespace PhotoAppApi.Services
{
    public class GroupService
    {
        private readonly AppDbContext _context;

        public GroupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueSlugAsync(string groupName)
        {
            // On génère le slug de base
            string baseSlug = SlugGenerator.GenerateSlug(groupName);

            // S'il est vide après nettoyage, on génère un fallback
            if (string.IsNullOrEmpty(baseSlug))
            {
                baseSlug = "group";
            }

            string uniqueSlug = baseSlug;

            // On vérifie dans la base de données si le slug existe déjà
            while (await _context.Groups.AnyAsync(g => g.ShortName == uniqueSlug))
            {
                // S'il y a une collision, on ajoute un petit ID de 5 caractères
                string shortId = Guid.NewGuid().ToString("N").Substring(0, 5);
                uniqueSlug = $"{baseSlug}-{shortId}";
            }

            return uniqueSlug;
        }
    }
}
