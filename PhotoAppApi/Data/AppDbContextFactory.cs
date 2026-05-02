using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PhotoAppApi.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1. Détecter l'environnement (Development par défaut)
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // 2. Construire la configuration pour lire TOUTES les sources
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddUserSecrets<AppDbContext>()
                .AddEnvironmentVariables() // <-- MAGIE : Permet de lire les variables injectées sous Linux
                .Build();

            // 3. Récupérer la chaîne
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 4. Sécurité anti-chaîne vide (remplace le "??")
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = "Server=localhost;Database=dummy;Uid=root;Pwd=dummy;";
            }

            var builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseMySql(connectionString, ServerVersion.Parse("10.5.0-mariadb"));

            return new AppDbContext(builder.Options);
        }
    }
}       