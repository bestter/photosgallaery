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

        public DbSet<PhotoLike> PhotoLikes { get; set; }

        public DbSet<PhotoView> PhotoViews { get; set; }

        // Nouvelles tables pour le système de Cercles/Groupes
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<GroupInvitation> GroupInvitations { get; set; }

        public DbSet<GroupRequest> GroupRequests { get; set; }

        // 2. On utilise l'API Fluent pour configurer la base de données
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Toujours appeler la méthode de base en premier
            base.OnModelCreating(modelBuilder);

            // Définition de la clé primaire composite pour TagTranslation
            modelBuilder.Entity<TagTranslation>()
                .HasKey(tt => new { tt.TagId, tt.Language });

            // Définition de la clé composite pour UserGroup
            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            // Relation Many-to-Many Groupes/Users
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);

            // Ajoute un index unique sur la colonne ShortName
            modelBuilder.Entity<Group>()
                .HasIndex(g => g.ShortName)
                .IsUnique();
        }
    }
}