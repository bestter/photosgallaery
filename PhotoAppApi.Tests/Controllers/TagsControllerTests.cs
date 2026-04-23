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
        public async Task SearchTags_WhenQueryIsNullOrWhiteSpace_ReturnsEmptyList(string query)
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
    }
}
