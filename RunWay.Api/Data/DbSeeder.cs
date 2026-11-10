using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RunWay.Api.Data;

public static class DbSeeder
{
    public const string AdminEmail = "admin@admin.com";
    public const string AdminPassword = "Admin@123";
    public const string OrganiserEmail = "organiser@raceday.com";
    public const string OrganiserPassword = "Organiser@123";
    public const string ParticipantEmail = "participant@raceday.com";
    public const string ParticipantPassword = "Participant@123";

    /// <summary>Creates the database (with retries while SQL Server starts), roles, admin and optional demo data.</summary>
    public static async Task InitialiseAsync(IServiceProvider services, bool seedDemoData, ILogger logger)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // SQL Server in Docker can take a while to accept connections.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (db.Database.GetMigrations().Any())
                    await db.Database.MigrateAsync();
                else
                    await db.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception ex) when (attempt < (db.Database.IsSqlServer() ? 30 : 1))
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/30): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                Check(await roleManager.CreateAsync(new IdentityRole(role)));
        }

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();
        var adminPassword = seedDemoData ? AdminPassword : config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminPassword))
            await EnsureUserAsync(users, config["Seed:AdminEmail"] ?? AdminEmail, adminPassword, "System", "Admin", Roles.Admin);

        if (seedDemoData && !await db.Events.AnyAsync())
            await SeedDemoDataAsync(db, users);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> users, string email,
        string password, string first, string last, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = first,
                LastName = last,
                Gender = Gender.PreferNotToSay
            };
            Check(await users.CreateAsync(user, password));
        }

        if (!await users.IsInRoleAsync(user, role))
            Check(await users.AddToRoleAsync(user, role));

        return user;
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        var organiser = await EnsureUserAsync(users, OrganiserEmail, OrganiserPassword, "Olivia", "Organiser", Roles.Organiser);
        var me = await EnsureUserAsync(users, ParticipantEmail, ParticipantPassword, "Pat", "Participant", Roles.Participant);

        var names = new[] { ("Thabo", "Mokoena"), ("Lerato", "Dlamini"), ("Sipho", "Nkosi"), ("Aisha", "Patel"), ("Liam", "van Wyk") };
        var runners = new List<ApplicationUser> { me };
        for (var i = 0; i < names.Length; i++)
        {
            runners.Add(await EnsureUserAsync(users, $"runner{i + 1}@raceday.com", "Runner@123",
                names[i].Item1, names[i].Item2, Roles.Participant));
        }

        var today = RunWay.Api.Services.RaceClock.Today;

        var events = new List<Event>
        {
            new() { Name = "Sunrise 10K Road Run", Description = "A fast, flat road race along the waterfront at sunrise. Perfect for chasing a personal best.",
                    Date = today.AddDays(21).AddHours(6), Location = "Sea Point Promenade, Cape Town", DistanceKm = 10, EventType = EventType.Run, OrganiserId = organiser.Id },
            new() { Name = "Drakensberg Trail Challenge", Description = "Technical single-track and big climbs through the foothills. Bring trail shoes and a sense of adventure.",
                    Date = today.AddDays(45).AddHours(7), Location = "Champagne Valley, KwaZulu-Natal", DistanceKm = 21, EventType = EventType.Run, OrganiserId = organiser.Id },
            new() { Name = "Spring Family Fun Run", Description = "Walk, jog or run with family and friends. Medals for every finisher!",
                    Date = today.AddDays(10).AddHours(8), Location = "Zoo Lake, Johannesburg", DistanceKm = 5, EventType = EventType.Walk, OrganiserId = organiser.Id },
            new() { Name = "City Lights Half Marathon", Description = "Last month's evening half marathon through the city centre.",
                    Date = today.AddDays(-30).AddHours(17), Location = "Durban Beachfront", DistanceKm = 21.1m, EventType = EventType.Run, OrganiserId = organiser.Id },
            new() { Name = "Winter Warm-Up 10K", Description = "A cold-morning classic that kicked off the season.",
                    Date = today.AddDays(-75).AddHours(7), Location = "Pretoria Botanical Gardens", DistanceKm = 10, EventType = EventType.Run, OrganiserId = organiser.Id },
        };
        db.Events.AddRange(events);
        await db.SaveChangesAsync();

        // Categories
        var categories = new Dictionary<int, List<Category>>();
        foreach (var e in events)
        {
            var list = e.DistanceKm switch
            {
                <= 5 => new List<Category> { new() { Name = "5 km Fun Run", DistanceKm = 5 }, new() { Name = "2 km Kids", DistanceKm = 2 } },
                <= 10 => new List<Category> { new() { Name = "10 km Race", DistanceKm = 10 }, new() { Name = "5 km Social", DistanceKm = 5 } },
                _ => new List<Category> { new() { Name = $"{e.DistanceKm:0.#} km Race", DistanceKm = e.DistanceKm }, new() { Name = "10 km Short", DistanceKm = 10 } }
            };
            foreach (var c in list) c.EventId = e.Id;
            db.Categories.AddRange(list);
            categories[e.Id] = list;
        }
        await db.SaveChangesAsync();

        // Enrolments (everyone in every event; some pending on upcoming events)
        var rng = new Random(42);
        foreach (var e in events)
        {
            var upcoming = e.Date > RunWay.Api.Services.RaceClock.Now;
            var enrolments = new List<Enrolment>();
            for (var i = 0; i < runners.Count; i++)
            {
                var status = !upcoming ? EnrolmentStatus.Confirmed
                    : i % 3 == 0 ? EnrolmentStatus.Pending : EnrolmentStatus.Confirmed;
                var en = new Enrolment
                {
                    EventId = e.Id,
                    CategoryId = categories[e.Id][i % 2].Id,
                    ParticipantId = runners[i].Id,
                    Status = status,
                    EnrolledAt = DateTime.UtcNow.AddDays(-rng.Next(5, 60))
                };
                enrolments.Add(en);
            }
            db.Enrolments.AddRange(enrolments);
            await db.SaveChangesAsync();

            // Results for past events
            if (!upcoming)
            {
                var ordered = enrolments.OrderBy(_ => rng.Next()).ToList();
                var pos = 1;
                var seconds = (int)(e.DistanceKm * 60 * 4.5m);
                foreach (var en in ordered)
                {
                    seconds += rng.Next(20, 240);
                    db.Results.Add(new RaceResult { EventId = e.Id, EnrolmentId = en.Id, Position = pos++, FinishTime = TimeSpan.FromSeconds(seconds) });
                }
                await db.SaveChangesAsync();
            }
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
