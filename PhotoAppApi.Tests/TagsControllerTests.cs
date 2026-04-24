using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoAppApi.Tests
{
    public class TagsControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task SearchTags_EmptyQuery_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(returnedTags);
        }

        [Fact]
        public async Task SearchTags_NullQuery_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(returnedTags);
        }

        [Fact]
        public async Task SearchTags_WhitespaceQuery_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("   ");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(returnedTags);
        }

        [Fact]
        public async Task SearchTags_ReturnsMatchingTags_ForFrenchLanguage()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var tag1 = new Tag { Id = 1 };
            var tag2 = new Tag { Id = 2 };
            context.Tags.AddRange(tag1, tag2);
            context.TagTranslations.Add(new TagTranslation { TagId = 1, Tag = tag1, Language = Language.FR, Name = "Nature" });
            context.TagTranslations.Add(new TagTranslation { TagId = 2, Tag = tag2, Language = Language.FR, Name = "Voiture" });
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("nat");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value).ToList();
            Assert.Single(tags);
            Assert.Contains("Nature", tags);
        }

        [Fact]
        public async Task SearchTags_DoesNotReturnNonFrenchTags()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var tag1 = new Tag { Id = 1 };
            context.Tags.Add(tag1);
            context.TagTranslations.Add(new TagTranslation { TagId = 1, Tag = tag1, Language = Language.EN, Name = "Nature" });
            var tag2 = new Tag { Id = 2 };
            context.Tags.Add(tag2);
            context.TagTranslations.Add(new TagTranslation { TagId = 2, Tag = tag2, Language = Language.ES, Name = "Naturaleza" });
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("nat");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value).ToList();
            Assert.Empty(tags);
        }

        [Fact]
        public async Task SearchTags_ReturnsMaximumTenDistinctTags()
        {
            // Arrange
            using var context = GetDatabaseContext();
            for (int i = 0; i < 15; i++)
            {
                var tag = new Tag { Id = i + 1 };
                context.Tags.Add(tag);
                // We create 15 distinct names containing "nat" to test Take(10)
                context.TagTranslations.Add(new TagTranslation { TagId = i + 1, Tag = tag, Language = Language.FR, Name = $"TagNat{i}" });
            }

            // Adding duplicates to test Distinct()
            var duplicateTag1 = new Tag { Id = 101 };
            var duplicateTag2 = new Tag { Id = 102 };
            context.Tags.AddRange(duplicateTag1, duplicateTag2);
            context.TagTranslations.Add(new TagTranslation { TagId = 101, Tag = duplicateTag1, Language = Language.FR, Name = "TagNat0" });
            context.TagTranslations.Add(new TagTranslation { TagId = 102, Tag = duplicateTag2, Language = Language.FR, Name = "TagNat0" });

            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("nat");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value).ToList();

            Assert.Equal(10, tags.Count);
            // Verify they are distinct
            Assert.Equal(tags.Count, tags.Distinct().Count());
        }

        [Fact]
        public async Task SearchTags_IsCaseInsensitive()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var tag1 = new Tag { Id = 1 };
            context.Tags.Add(tag1);
            context.TagTranslations.Add(new TagTranslation { TagId = 1, Tag = tag1, Language = Language.FR, Name = "NATURE" });
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("nat");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value).ToList();
            Assert.Single(tags);
            Assert.Contains("NATURE", tags);
        }

        [Fact]
        public async Task SearchTags_ThrowsException_Returns500()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);
            context.Dispose(); // Dispose it so any query will throw ObjectDisposedException

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("test");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);

            var value = statusCodeResult.Value;
            Assert.NotNull(value);
            var propertyInfo = value.GetType().GetProperty("message");
            Assert.NotNull(propertyInfo);
            var message = propertyInfo.GetValue(value) as string;
            Assert.Equal("Erreur lors de la recherche de tags.", message);
        }
    }
}
