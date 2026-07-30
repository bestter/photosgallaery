using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class OpenApiContractTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OpenApiContractTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
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
                        { "ObjectStorage:Region", "eu-west-1" },
                        { "ObjectStorage:AccessKey", "test" },
                        { "ObjectStorage:SecretKey", "test" },
                        { "ObjectStorage:ServiceUrl", "http://localhost:9000" },
                        { "ObjectStorage:BucketName", "test-bucket" },
                        { "FrontendUrl", "http://localhost:3000" },
                        { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=testdb;User=root;Password=root;" },
                        { "Jwt:Key", "une_super_cle_secrete_pour_les_tests_qui_doit_etre_vraiment_tres_longue_12345678901234567890!" }
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<Data.AppDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<Data.AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForOpenApiTesting");
                    });
                });
            });
        }

        [Fact]
        public async Task SwaggerEndpoint_ReturnsValidOpenApiContractJson()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Contains("application/json", response.Content.Headers.ContentType.ToString());

            var jsonString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Verify OpenAPI document structure
            Assert.True(root.TryGetProperty("openapi", out var openapiProp) || root.TryGetProperty("swagger", out openapiProp), "OpenAPI version missing.");
            Assert.True(root.TryGetProperty("info", out var infoProp), "OpenAPI info section missing.");
            Assert.True(infoProp.TryGetProperty("title", out _), "OpenAPI title missing.");

            // Verify paths element exists
            Assert.True(root.TryGetProperty("paths", out var pathsProp), "OpenAPI paths section missing.");
            
            // Validate presence of core controller routes in OpenAPI contract
            var pathKeys = pathsProp.EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();
            Assert.Contains(pathKeys, p => p.Contains("/api/auth"));
            Assert.Contains(pathKeys, p => p.Contains("/api/photos"));
            Assert.Contains(pathKeys, p => p.Contains("/api/admin/groups") || p.Contains("/api/admin"));
            Assert.Contains(pathKeys, p => p.Contains("/api/contact"));
        }

        [Fact]
        public async Task OpenApiContract_DefinesBearerAuthSecurityScheme()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/v1/swagger.json", TestContext.Current.CancellationToken);
            var jsonString = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Assert securitySchemes section in components
            Assert.True(root.TryGetProperty("components", out var componentsProp), "Components section missing.");
            Assert.True(componentsProp.TryGetProperty("securitySchemes", out var securitySchemesProp), "securitySchemes section missing.");
            Assert.True(securitySchemesProp.TryGetProperty("bearerAuth", out var bearerAuthProp), "bearerAuth scheme definition missing.");

            Assert.Equal("http", bearerAuthProp.GetProperty("type").GetString());
            Assert.Equal("bearer", bearerAuthProp.GetProperty("scheme").GetString());
            Assert.Equal("JWT", bearerAuthProp.GetProperty("bearerFormat").GetString());
        }
    }
}
