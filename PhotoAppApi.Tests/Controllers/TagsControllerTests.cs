using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace PhotoAppApi.Tests.Controllers
{
    public class TagsControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchTags_WhenQueryIsNullOrWhiteSpace_ReturnsEmptyList(string? query)
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(returnedTags);
        }

        [Fact]
        public async Task SearchTags_EmptyQuery_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetDbContext();
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
            using var context = GetDbContext();
            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Empty(returnedTags);
        }

        [Fact]
        public async Task SearchTags_WhenQueryMatches_ReturnsSuggestionsInFrench()
        {
            // Arrange
            using var context = GetDbContext();
            var tag1 = new Models.Tag { Id = 1, IsActive = true };
            var tag2 = new Models.Tag { Id = 2, IsActive = true };
            context.Tags.AddRange(tag1, tag2);
            context.TagTranslations.AddRange(
                new Models.TagTranslation { TagId = 1, Language = Models.Language.FR, Name = "Nature" },
                new Models.TagTranslation { TagId = 1, Language = Models.Language.EN, Name = "Nature-En" },
                new Models.TagTranslation { TagId = 2, Language = Models.Language.FR, Name = "Natation" }
            );
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("nat");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Collection(returnedTags,
                t => Assert.Equal("Nature", t),
                t => Assert.Equal("Natation", t));
        }

        [Fact]
        public async Task SearchTags_ReturnsDistinctSuggestions()
        {
            // Arrange
            using var context = GetDbContext();
            var tag1 = new Models.Tag { Id = 1, IsActive = true };
            var tag2 = new Models.Tag { Id = 2, IsActive = true };
            context.Tags.AddRange(tag1, tag2);
            context.TagTranslations.AddRange(
                new Models.TagTranslation { TagId = 1, Language = Models.Language.FR, Name = "Fleur" },
                new Models.TagTranslation { TagId = 2, Language = Models.Language.FR, Name = "Fleur" }
            );
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("fle");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            var tagsList = new List<string>(returnedTags);
            Assert.Single(tagsList);
            Assert.Equal("Fleur", tagsList[0]);
        }

        [Fact]
        public async Task SearchTags_ReturnsMaxTenSuggestions()
        {
            // Arrange
            using var context = GetDbContext();
            for (int i = 1; i <= 15; i++)
            {
                var tag = new Models.Tag { Id = i, IsActive = true };
                context.Tags.Add(tag);
                context.TagTranslations.Add(new Models.TagTranslation { TagId = i, Language = Models.Language.FR, Name = $"Arbre {i}" });
            }
            await context.SaveChangesAsync();

            var controller = new TagsController(context);

            // Act
            var result = await controller.SearchTags("arb");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTags = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            var tagsList = new List<string>(returnedTags);
            Assert.Equal(10, tagsList.Count);
        }
    }
}
