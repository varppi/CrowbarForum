using Crowbar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace Crowbar.Data
{
    public class ApplicationDbContext : IdentityDbContext<CrowbarUser>
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<CrowbarUser>().HasIndex(u => u.UserName).IsUnique();
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Models.File> Files { get; set; }
        public DbSet<Models.Thread> Threads { get; set; }
        public DbSet<Models.Comment> Comments { get; set; }
        public DbSet<Models.Category> Categories { get; set; }
        public DbSet<Models.SiteSettings> SiteSettings { get; set; }
    }
}
