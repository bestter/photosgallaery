using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SecurityHeadersTests(WebApplicationFactory<Program> factory)
        {
            // Configure factory to avoid startup crashes due to missing appsettings
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("FrontendUrl", "http://localhost:3000");
                builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=localhost;Database=testdb;User=root;Password=root;");
                builder.UseSetting("Jwt:Key", "une_super_cle_secrete_pour_les_tests_qui_doit_etre_vraiment_tres_longue_12345678901234567890!");
                builder.UseSetting("ObjectStorage:Region", "eu-west-1");
                builder.UseSetting("ObjectStorage:AccessKey", "test");
                builder.UseSetting("ObjectStorage:SecretKey", "test");
                builder.UseSetting("ObjectStorage:ServiceUrl", "https://s3.amazonaws.com");
                builder.UseSetting("ObjectStorage:BucketName", "test-bucket");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        {"ObjectStorage:Region", "eu-west-1"},
                        {"ObjectStorage:AccessKey", "test"},
                        {"ObjectStorage:SecretKey", "test"},
                        {"ObjectStorage:ServiceUrl", "http://localhost:9000"},
                        {"ObjectStorage:BucketName", "test-bucket"},
                        { "FrontendUrl", "http://localhost:3000" },
                        { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=testdb;User=root;Password=root;" },
                        { "Jwt:Key", "une_super_cle_secrete_pour_les_tests_qui_doit_etre_vraiment_tres_longue_12345678901234567890!" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext configuration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<Data.AppDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database
                    services.AddDbContext<Data.AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });
                });
            });
        }

        [Fact]
        public async Task Middleware_Adds_Correct_ContentSecurityPolicy_Header()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/", TestContext.Current.CancellationToken); // Will hit fallback to index.html or 404, but middleware still applies

            // Assert
            Assert.True(response.Headers.Contains("Content-Security-Policy"), "L'en-tête Content-Security-Policy est manquant.");

            var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();

            // Check Google Tag Manager
            Assert.Contains("https://www.googletagmanager.com", cspHeader);

            // Check Google Fonts
            Assert.Contains("https://fonts.googleapis.com", cspHeader);
        }
    }
}
