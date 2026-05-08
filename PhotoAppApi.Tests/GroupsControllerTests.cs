using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Controllers;
using PhotoAppApi.Data;
using PhotoAppApi.Models;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class GroupsControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = string.Empty,
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsNull()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = null!,
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task CreateGroup_ReturnsBadRequest_WhenNameIsWhitespace()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new CreateGroupRequest
            {
                Name = "   ",
                Description = "A description"
            };

            // Act
            var result = await controller.CreateGroup(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
            var messageProperty = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value, null) as string;
            Assert.Equal("Le nom du groupe est requis.", messageProperty);
        }

        [Fact]
        public async Task DeleteGroup_ReturnsNotFound_WhenGroupDoesNotExist()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await controller.DeleteGroup(nonExistentId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAllGroups_ReturnsOkResult_WithGroups_SortedByCreatedAtDescending()
        {
            // Arrange
            using var context = GetDatabaseContext();

            var group1 = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Group 1",
                ShortName = "group-1",
                Description = "A test group",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            var group2 = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Group 2",
                ShortName = "group-2",
                Description = "Another test group",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash",
                Role = UserRole.User
            };

            var userGroup = new UserGroup
            {
                GroupId = group2.Id,
                UserId = user.Id,
                Group = group2,
                User = user,
                Role = GroupUserRole.Member
            };

            var photo = new Photo
            {
                Id = 1,
                FileName = "test.jpg",
                Url = "/test.jpg",
                FileHash = "hash",
                UploaderUsername = user.Username,
                GroupId = group2.Id,
                Group = group2
            };

            context.Groups.AddRange(group1, group2);
            context.Users.Add(user);
            context.UserGroups.Add(userGroup);
            context.Photos.Add(photo);
            await context.SaveChangesAsync();

            var controller = new GroupsController(context);

            // Act
            var result = await controller.GetAllGroups();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);

            var resultList = new List<object>();
            foreach(var item in enumerable)
            {
                resultList.Add(item);
            }

            Assert.Equal(2, resultList.Count);

            // Should be sorted by CreatedAt descending, so group2 first
            var firstItem = resultList[0];
            var firstItemName = firstItem.GetType().GetProperty("Name")?.GetValue(firstItem, null) as string;
            var firstItemUserCount = (int)(firstItem.GetType().GetProperty("UserCount")?.GetValue(firstItem, null) ?? 0);
            var firstItemPhotoCount = (int)(firstItem.GetType().GetProperty("PhotoCount")?.GetValue(firstItem, null) ?? 0);

            Assert.Equal("Group 2", firstItemName);
            Assert.Equal(1, firstItemUserCount);
            Assert.Equal(1, firstItemPhotoCount);

            var secondItem = resultList[1];
            var secondItemName = secondItem.GetType().GetProperty("Name")?.GetValue(secondItem, null) as string;
            var secondItemUserCount = (int)(secondItem.GetType().GetProperty("UserCount")?.GetValue(secondItem, null) ?? 0);
            var secondItemPhotoCount = (int)(secondItem.GetType().GetProperty("PhotoCount")?.GetValue(secondItem, null) ?? 0);

            Assert.Equal("Group 1", secondItemName);
            Assert.Equal(0, secondItemUserCount);
            Assert.Equal(0, secondItemPhotoCount);
        }

        [Fact]
        public async Task GetAllGroups_ReturnsOkResult_WhenNoGroupsExist()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);

            // Act
            var result = await controller.GetAllGroups();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);

            var resultList = new List<object>();
            foreach(var item in enumerable)
            {
                resultList.Add(item);
            }

            Assert.Empty(resultList);
        }

        [Fact]
        public async Task DeleteGroup_ReturnsOk_WhenGroupExists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Test Group",
                ShortName = "test-group",
                Description = "A test group"
            };
            context.Groups.Add(group);
            await context.SaveChangesAsync();
            var controller = new GroupsController(context);

            // Act
            var result = await controller.DeleteGroup(group.Id);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var deletedGroup = await context.Groups.FindAsync(group.Id);
            Assert.Null(deletedGroup);
        }
    }
}
