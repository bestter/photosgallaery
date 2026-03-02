using Microsoft.EntityFrameworkCore;
using PhotoAppApi.Models;
using System.Collections.Generic;

namespace PhotoAppApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Photo> Photos { get; set; }


        public DbSet<User> Users { get; set; }
    }
}