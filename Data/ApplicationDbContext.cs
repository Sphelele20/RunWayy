using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RunWay.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Store the Gender enum as a string in the database
            builder.Entity<ApplicationUser>()
                .Property(u => u.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}