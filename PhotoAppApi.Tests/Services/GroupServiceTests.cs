using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class GroupServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly GroupService _groupService;

        public GroupServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _groupService = new GroupService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GenerateUniqueSlugAsync_GeneratesNormalSlug()
        {
            // Arrange
            string groupName = "My Awesome Group";

            // Act
            string slug = await _groupService.GenerateUniqueSlugAsync(groupName, CancellationToken.None);

            // Assert
            Assert.Equal("my-awesome-group", slug);
        }

        [Fact]
        public async Task GenerateUniqueSlugAsync_UsesFallbackWhenSlugIsEmpty()
        {
            // Arrange
            string groupName = "---!!!???---"; // SlugGenerator will make this empty

            // Act
            string slug = await _groupService.GenerateUniqueSlugAsync(groupName, CancellationToken.None);

            // Assert
            Assert.Equal("group", slug);
        }

        [Fact]
        public async Task GenerateUniqueSlugAsync_HandlesSingleCollision()
        {
            // Arrange
            string groupName = "Test Group";
            string expectedBaseSlug = "test-group";

            _context.Groups.Add(new Group
            {
                Name = "Test Group 1",
                ShortName = expectedBaseSlug,
                Description = "Existing group"
            });
            await _context.SaveChangesAsync(CancellationToken.None);

            // Act
            string slug = await _groupService.GenerateUniqueSlugAsync(groupName, CancellationToken.None);

            // Assert
            Assert.StartsWith($"{expectedBaseSlug}-", slug);
            Assert.True(slug.Length > expectedBaseSlug.Length + 1); // Ensures suffix is added
        }

        [Fact]
        public async Task GenerateUniqueSlugAsync_HandlesMultipleCollisions()
        {
            // Arrange
            string groupName = "Collision Group";
            string expectedBaseSlug = "collision-group";

            _context.Groups.Add(new Group
            {
                Name = "C1",
                ShortName = expectedBaseSlug,
                Description = "D1"
            });
            await _context.SaveChangesAsync(CancellationToken.None);

            // Act
            string slug1 = await _groupService.GenerateUniqueSlugAsync(groupName, CancellationToken.None);

            // To simulate second collision, add the first generated slug
            _context.Groups.Add(new Group
            {
                Name = "C2",
                ShortName = slug1,
                Description = "D2"
            });
            await _context.SaveChangesAsync(CancellationToken.None);

            string slug2 = await _groupService.GenerateUniqueSlugAsync(groupName, CancellationToken.None);

            // Assert
            Assert.NotEqual(expectedBaseSlug, slug1);
            Assert.NotEqual(expectedBaseSlug, slug2);
            Assert.NotEqual(slug1, slug2);
            Assert.StartsWith($"{expectedBaseSlug}-", slug1);
            Assert.StartsWith($"{expectedBaseSlug}-", slug2);
        }
    }
}
