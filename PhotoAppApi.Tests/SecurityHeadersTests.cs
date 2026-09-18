using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                builder.ConfigureLogging(logging => logging.ClearProviders());

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
        public async Task Middleware_Adds_Strict_ContentSecurityPolicy_Header()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/", TestContext.Current.CancellationToken); // Will hit fallback to index.html or 404, but middleware still applies

            // Assert
            Assert.True(response.Headers.Contains("Content-Security-Policy"), "L'en-tête Content-Security-Policy est manquant.");

            var scriptSources = GetCspDirective(response, "script-src");

            Assert.Contains("https://www.googletagmanager.com", scriptSources);
            Assert.DoesNotContain("'unsafe-inline'", scriptSources);
            Assert.DoesNotContain("'unsafe-eval'", scriptSources);

            // Check Google Fonts
            var cspHeader = response.Headers.GetValues("Content-Security-Policy").First();
            Assert.Contains("https://fonts.googleapis.com", cspHeader);
        }

        [Fact]
        public async Task Middleware_Allows_InlineScripts_Only_For_Swagger_In_Development()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var swaggerResponse = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);
            var swaggerContractResponse = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
            var regularResponse = await client.GetAsync("/", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);

            var swaggerScriptSources = GetCspDirective(swaggerResponse, "script-src");
            Assert.Contains("'unsafe-inline'", swaggerScriptSources);
            Assert.DoesNotContain("'unsafe-eval'", swaggerScriptSources);

            var swaggerContractScriptSources = GetCspDirective(swaggerContractResponse, "script-src");
            Assert.DoesNotContain("'unsafe-inline'", swaggerContractScriptSources);
            Assert.DoesNotContain("'unsafe-eval'", swaggerContractScriptSources);

            var regularScriptSources = GetCspDirective(regularResponse, "script-src");
            Assert.DoesNotContain("'unsafe-inline'", regularScriptSources);
            Assert.DoesNotContain("'unsafe-eval'", regularScriptSources);
        }

        [Fact]
        public async Task Middleware_Does_Not_Allow_InlineScripts_For_Swagger_In_Production()
        {
            // Arrange
            using var productionFactory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
            using var client = productionFactory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html", TestContext.Current.CancellationToken);

            // Assert
            var scriptSources = GetCspDirective(response, "script-src");
            Assert.DoesNotContain("'unsafe-inline'", scriptSources);
            Assert.DoesNotContain("'unsafe-eval'", scriptSources);
        }

        private static string GetCspDirective(HttpResponseMessage response, string directiveName)
        {
            var cspHeader = response.Headers.GetValues("Content-Security-Policy").Single();

            return cspHeader
                .Split(';', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Single(directive => directive.StartsWith($"{directiveName} ", System.StringComparison.Ordinal));
        }
    }
}
