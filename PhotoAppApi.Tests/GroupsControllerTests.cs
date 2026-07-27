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
            var result = await controller.CreateGroup(request, TestContext.Current.CancellationToken);

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
            var result = await controller.CreateGroup(request, TestContext.Current.CancellationToken);

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
            var result = await controller.CreateGroup(request, TestContext.Current.CancellationToken);

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
            var result = await controller.DeleteGroup(nonExistentId, TestContext.Current.CancellationToken);

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
                Group = group2,
                UserId = user.Id,
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
                Group = group2,
                ThumbnailUrl = string.Empty
            };

            context.Groups.AddRange(group1, group2);
            context.Users.Add(user);
            context.UserGroups.Add(userGroup);
            context.Photos.Add(photo);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupsController(context);

            // Act
            var result = await controller.GetAllGroups(1, 20, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);

            var resultList = new List<object>();
            foreach (var item in enumerable)
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
            var result = await controller.GetAllGroups(1, 20, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);

            var resultList = new List<object>();
            foreach (var item in enumerable)
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
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            var controller = new GroupsController(context);

            // Act
            var result = await controller.DeleteGroup(group.Id, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var deletedGroup = await context.Groups.FindAsync(new object[] { group.Id }, TestContext.Current.CancellationToken);
            Assert.Null(deletedGroup);
        }

        [Fact]
        public async Task UpdateMemberRole_ReturnsNotFound_WhenMemberDoesNotExist()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new GroupsController(context);
            var request = new UpdateMemberRoleRequest
            {
                Role = GroupUserRole.Admin
            };

            // Act
            var result = await controller.UpdateMemberRole(Guid.NewGuid(), 999, request, TestContext.Current.CancellationToken);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
            var messageProperty = notFoundResult.Value.GetType().GetProperty("message")?.GetValue(notFoundResult.Value, null) as string;
            Assert.Equal("Membre non trouvé dans ce groupe.", messageProperty);
        }

        [Fact]
        public async Task UpdateMemberRole_ReturnsOk_WhenMemberExists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var group = new Group { Id = Guid.NewGuid(), Name = "Group", ShortName = "grp", Description = "Desc" };
            var user = new User { Id = 1, Username = "user", Email = "u@e.com", PasswordHash = "hash", Role = UserRole.User };
            var userGroup = new UserGroup { GroupId = group.Id, Group = group, UserId = user.Id, User = user, Role = GroupUserRole.Member };

            context.Groups.Add(group);
            context.Users.Add(user);
            context.UserGroups.Add(userGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupsController(context);
            var request = new UpdateMemberRoleRequest
            {
                Role = GroupUserRole.Admin
            };

            // Act
            var result = await controller.UpdateMemberRole(group.Id, user.Id, request, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var updatedUserGroup = await context.UserGroups.FirstOrDefaultAsync(ug => ug.GroupId == group.Id && ug.UserId == user.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedUserGroup);
            Assert.Equal(GroupUserRole.Admin, updatedUserGroup.Role);
        }

        [Fact]
        public async Task GetGroupMembers_ReturnsOkResult_WithMembers()
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
                GroupId = group.Id,
                Group = group,
                UserId = user.Id,
                User = user,
                Role = GroupUserRole.Admin,
                JoinedAt = DateTime.UtcNow
            };

            // Memory Rule check EF Core edge cases: seed with unrelated records
            var unrelatedGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Unrelated Group",
                ShortName = "unrelated-group",
                Description = "Unrelated"
            };
            var unrelatedUser = new User
            {
                Id = 2,
                Username = "unrelated",
                Email = "unrelated@example.com",
                PasswordHash = "hash",
                Role = UserRole.User
            };
            var unrelatedUserGroup = new UserGroup
            {
                GroupId = unrelatedGroup.Id,
                Group = unrelatedGroup,
                UserId = unrelatedUser.Id,
                User = unrelatedUser,
                Role = GroupUserRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            context.Groups.AddRange(group, unrelatedGroup);
            context.Users.AddRange(user, unrelatedUser);
            context.UserGroups.AddRange(userGroup, unrelatedUserGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupsController(context);

            // Act
            var result = await controller.GetGroupMembers(group.Id, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            Assert.NotNull(enumerable);

            var resultList = new List<object>();
            foreach (var item in enumerable)
            {
                resultList.Add(item);
            }

            Assert.Single(resultList);
            var firstItem = resultList[0];
            var firstItemUserId = (int)(firstItem.GetType().GetProperty("UserId")?.GetValue(firstItem, null) ?? 0);
            var firstItemUsername = firstItem.GetType().GetProperty("Username")?.GetValue(firstItem, null) as string;
            var firstItemEmail = firstItem.GetType().GetProperty("Email")?.GetValue(firstItem, null) as string;
            var firstItemRole = (GroupUserRole)(firstItem.GetType().GetProperty("Role")?.GetValue(firstItem, null) ?? GroupUserRole.Member);

            Assert.Equal(1, firstItemUserId);
            Assert.Equal("testuser", firstItemUsername);
            Assert.Equal("test@example.com", firstItemEmail);
            Assert.Equal(GroupUserRole.Admin, firstItemRole);
        }

        [Fact]
        public async Task GetGroupMembers_ReturnsOkResult_WhenNoMembersExist()
        {
            // Arrange
            using var context = GetDatabaseContext();

            // Memory Rule check EF Core edge cases: seed with unrelated records
            var unrelatedGroup = new Group
            {
                Id = Guid.NewGuid(),
                Name = "Unrelated Group",
                ShortName = "unrelated-group",
                Description = "Unrelated"
            };
            var unrelatedUser = new User
            {
                Id = 2,
                Username = "unrelated",
                Email = "unrelated@example.com",
                PasswordHash = "hash",
                Role = UserRole.User
            };
            var unrelatedUserGroup = new UserGroup
            {
                GroupId = unrelatedGroup.Id,
                Group = unrelatedGroup,
                UserId = unrelatedUser.Id,
                User = unrelatedUser,
                Role = GroupUserRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            context.Groups.Add(unrelatedGroup);
            context.Users.Add(unrelatedUser);
            context.UserGroups.Add(unrelatedUserGroup);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var controller = new GroupsController(context);
            var groupId = Guid.NewGuid(); // non-existent or no members

            // Act
            var result = await controller.GetGroupMembers(groupId, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            Assert.NotNull(resultValue);

            var enumerable = resultValue as System.Collections.IEnumerable;
            var resultList = new List<object>();
            if (enumerable != null) {
                foreach(var item in enumerable) { resultList.Add(item); }
            }
            Assert.Empty(resultList);
        }
    }
}
