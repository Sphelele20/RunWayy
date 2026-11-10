using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RunWay.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        if (Database.IsSqlite()) {
            b.Entity<Event>().Property(x => x.DistanceKm).HasConversion<double>();
            b.Entity<Category>().Property(x => x.DistanceKm).HasConversion<double>();
            b.Entity<RaceResult>().Property(x => x.FinishTime).HasConversion<long>();
        }

        b.Entity<ApplicationUser>().Property(u => u.Gender).HasConversion<string>().HasMaxLength(20);

        b.Entity<Event>(e =>
        {
            e.Property(x => x.DistanceKm).HasPrecision(8, 2);
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Date);
            e.HasOne(x => x.Organiser).WithMany().HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Category>(c =>
        {
            c.Property(x => x.DistanceKm).HasPrecision(8, 2);
            c.HasIndex(x => new { x.EventId, x.Name }).IsUnique();
            c.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            // One enrolment per participant per event.
            e.HasIndex(x => new { x.EventId, x.ParticipantId }).IsUnique();

            e.HasOne(x => x.Event).WithMany(x => x.Enrolments).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            // NoAction here avoids SQL Server "multiple cascade paths" errors.
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Participant).WithMany().HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<RaceResult>(r =>
        {
            r.HasIndex(x => x.EnrolmentId).IsUnique();
            r.HasIndex(x => new { x.EventId, x.Position }).IsUnique();
            r.HasOne<Event>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<RaceResult>(x => x.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
