using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhotoAppApi.Data
{
    // Cette classe sert UNIQUEMENT à la génération du bundle et aux outils de migration
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();

            // Chaîne de connexion fictive. 
            // Lors du déploiement, le script PowerShell écrase ceci avec l'argument --connection
            var dummyConnectionString = "Server=localhost;Database=dummy;Uid=root;Pwd=dummy;";

            // On utilise une version statique de MariaDB pour éviter qu'EF Core n'essaie 
            // de se connecter à la base de données "dummy" pendant la compilation.
            builder.UseMySql(dummyConnectionString, ServerVersion.Parse("10.5.0-mariadb"));

            return new AppDbContext(builder.Options);
        }
    }
}