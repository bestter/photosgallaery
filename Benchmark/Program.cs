using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// Dummy classes
public class User {
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // adding padding to simulate large rows
    public string Data1 { get; set; } = new string('x', 1000);
    public string Data2 { get; set; } = new string('x', 1000);
    public string Data3 { get; set; } = new string('x', 1000);
}

public class Group {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserGroup {
    public int UserId { get; set; }
    public int GroupId { get; set; }
}

public class AppDbContext : DbContext {
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseInMemoryDatabase("TestDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<UserGroup>().HasKey(ug => new { ug.UserId, ug.GroupId });
    }
}

class Program {
    static async Task Main(string[] args) {
        using var setupContext = new AppDbContext();
        setupContext.Database.EnsureDeleted();
        setupContext.Database.EnsureCreated();

        Console.WriteLine("Seeding database...");
        var defaultGroup = new Group { Id = 1, Name = "Cercle Initial" };
        setupContext.Groups.Add(defaultGroup);

        var users = Enumerable.Range(1, 10000).Select(i => new User { Id = i, Username = $"User{i}", Email = $"user{i}@test.com" }).ToList();
        setupContext.Users.AddRange(users);

        // Seed some user groups
        var userGroups = Enumerable.Range(1, 5000).Select(i => new UserGroup { UserId = i, GroupId = defaultGroup.Id }).ToList();
        setupContext.UserGroups.AddRange(userGroups);
        await setupContext.SaveChangesAsync();

        Console.WriteLine("Running unoptimized current implementation...");

        using var unoptimizedContext = new AppDbContext();

        var sw = Stopwatch.StartNew();

        // 2. Assigner tous les utilisateurs existants à ce groupe
        var allUsers = await unoptimizedContext.Users.ToListAsync();
        var existingUserIdsInGroup = await unoptimizedContext.UserGroups
            .Where(ug => ug.GroupId == defaultGroup.Id)
            .Select(ug => ug.UserId)
            .ToListAsync();
        var existingUserIdsSet = new HashSet<int>(existingUserIdsInGroup);

        foreach (var user in allUsers)
        {
            if (!existingUserIdsSet.Contains(user.Id))
            {
                unoptimizedContext.UserGroups.Add(new UserGroup { UserId = user.Id, GroupId = defaultGroup.Id });
            }
        }
        await unoptimizedContext.SaveChangesAsync();

        sw.Stop();
        Console.WriteLine($"Baseline Time: {sw.ElapsedMilliseconds} ms");

        // Setup again for optimized benchmark
        using var setupContext2 = new AppDbContext();
        setupContext2.Database.EnsureDeleted();
        setupContext2.Database.EnsureCreated();
        setupContext2.Groups.Add(new Group { Id = 1, Name = "Cercle Initial" });
        setupContext2.Users.AddRange(users);
        setupContext2.UserGroups.AddRange(userGroups);
        await setupContext2.SaveChangesAsync();

        Console.WriteLine("Running fully optimized implementation...");
        using var optimizedContext = new AppDbContext();

        var sw2 = Stopwatch.StartNew();

        var allUserIds = await optimizedContext.Users.Select(u => u.Id).ToListAsync();
        var existingUserIdsInGroup2 = await optimizedContext.UserGroups
            .Where(ug => ug.GroupId == defaultGroup.Id)
            .Select(ug => ug.UserId)
            .ToListAsync();
        var existingUserIdsSet2 = new HashSet<int>(existingUserIdsInGroup2);

        var newMemberships = new List<UserGroup>();
        foreach (var userId in allUserIds)
        {
            if (!existingUserIdsSet2.Contains(userId))
            {
                newMemberships.Add(new UserGroup { UserId = userId, GroupId = defaultGroup.Id });
            }
        }
        optimizedContext.UserGroups.AddRange(newMemberships);
        await optimizedContext.SaveChangesAsync();

        sw2.Stop();
        Console.WriteLine($"Optimized Time: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"Improvement: {sw.ElapsedMilliseconds - sw2.ElapsedMilliseconds} ms");
    }
}
