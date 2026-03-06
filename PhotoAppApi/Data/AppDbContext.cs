using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Models;

namespace PhotoAppApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Photo> Photos { get; set; }
        public DbSet<ImageReport> ImageReports { get; set; }
        public DbSet<User> Users { get; set; }

        // 1. On déclare les nouvelles tables à Entity Framework
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagTranslation> TagTranslations { get; set; }

        // 2. On utilise l'API Fluent pour configurer la base de données
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Toujours appeler la méthode de base en premier
            base.OnModelCreating(modelBuilder);

            // Définition de la clé primaire composite pour TagTranslation
            modelBuilder.Entity<TagTranslation>()
                .HasKey(tt => new { tt.TagId, tt.Language });

            // C'est aussi ici que tu pourrais forcer Entity Framework à sauvegarder 
            // ton enum UserRole sous forme de string ("Creator", "Admin") plutôt 
            // qu'en entier (1, 9999) dans MariaDB, si tu le souhaites un jour !
        }
    }
}