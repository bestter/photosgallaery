using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoAppApi.Data
{
    // Cette classe sert UNIQUEMENT à la génération du bundle et aux outils de migration
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1. On configure la lecture des fichiers appsettings
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // 2. On va chercher la vraie chaîne de connexion (assure-toi que le nom correspond à ton appsettings)
            // S'il ne la trouve pas (ex: pendant ton script PowerShell), il garde le "dummy"
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? "Server=localhost;Database=dummy;Uid=root;Pwd=dummy;";

            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // 3. On utilise la chaîne trouvée
            builder.UseMySql(connectionString, ServerVersion.Parse("10.5.0-mariadb"));

            return new AppDbContext(builder.Options);
        }
    }
}