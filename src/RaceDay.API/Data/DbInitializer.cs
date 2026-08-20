using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(RaceDayDbContext context, IPasswordHasher passwordHasher)
    {
        // Apply migrations if using relational provider
        if (context.Database.IsRelational())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (await context.Users.AnyAsync())
        {
            return; // DB already seeded
        }

        var defaultPasswordHash = passwordHasher.HashPassword("Password123!");

        // 1. Seed Users (2 Organisers, 3 Participants)
        var organiser1 = new User
        {
            FullName = "Sarah Jenkins",
            Email = "sarah.jenkins@raceday.co.za",
            PasswordHash = defaultPasswordHash,
            Role = "Organiser",
            PhoneNumber = "+27 82 555 0101",
            ProfilePictureUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=400&q=80",
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        var organiser2 = new User
        {
            FullName = "David Khumalo",
            Email = "david.khumalo@raceday.co.za",
            PasswordHash = defaultPasswordHash,
            Role = "Organiser",
            PhoneNumber = "+27 83 444 0202",
            ProfilePictureUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&q=80",
            CreatedAt = DateTime.UtcNow.AddDays(-55)
        };

        var participant1 = new User
        {
            FullName = "Lerato Sithole",
            Email = "lerato.sithole@gmail.com",
            PasswordHash = defaultPasswordHash,
            Role = "Participant",
            PhoneNumber = "+27 71 222 0303",
            ProfilePictureUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&q=80",
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        };

        var participant2 = new User
        {
            FullName = "Michael van der Merwe",
            Email = "michael.vdm@outlook.com",
            PasswordHash = defaultPasswordHash,
            Role = "Participant",
            PhoneNumber = "+27 84 333 0404",
            ProfilePictureUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80",
            CreatedAt = DateTime.UtcNow.AddDays(-35)
        };

        var participant3 = new User
        {
            FullName = "Jarred Naidoo",
            Email = "jarred.naidoo@varsitycollege.co.za",
            PasswordHash = defaultPasswordHash,
            Role = "Participant",
            PhoneNumber = "+27 81 777 0505",
            ProfilePictureUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&q=80",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        context.Users.AddRange(organiser1, organiser2, participant1, participant2, participant3);
        await context.SaveChangesAsync();

        // 2. Seed Events (3 diverse disciplines: Run, Walk, Cycle)
        var event1 = new Event
        {
            OrganiserId = organiser1.UserId,
            Title = "Cape Peninsula Coastal Marathon 2026",
            Description = "Experience the premier scenic marathon tracing the breathtaking Atlantic Seaboard and Chapman's Peak Drive. Fully marshalled with hydration stations every 3km.",
            EventDate = DateTime.UtcNow.AddDays(25),
            Location = "Camps Bay Promenade, Cape Town",
            DistanceKm = 42.20m,
            EventType = "Run",
            BannerImageUrl = "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=1200&q=80",
            Status = "Upcoming",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var event2 = new Event
        {
            OrganiserId = organiser2.UserId,
            Title = "Durban Golden Mile Wellness Walk 2026",
            Description = "A vibrant family and community fitness walk along the sunny Durban beachfront promenade. Perfect for all ages, walkers, and strollers with commemorative medals.",
            EventDate = DateTime.UtcNow.AddDays(35),
            Location = "Suncoast Boardwalk, Durban",
            DistanceKm = 5.00m,
            EventType = "Walk",
            BannerImageUrl = "https://images.unsplash.com/photo-1502680390469-be75c86b636f?w=1200&q=80",
            Status = "Upcoming",
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        var event3 = new Event
        {
            OrganiserId = organiser1.UserId,
            Title = "Cradle of Humankind Cycle Challenge",
            Description = "A high-octane road cycling classic winding through the rolling hills of the UNESCO World Heritage Cradle of Humankind. Full mechanical support available.",
            EventDate = DateTime.UtcNow.AddDays(-14),
            Location = "Maropeng Visitors Centre, Gauteng",
            DistanceKm = 94.70m,
            EventType = "Cycle",
            BannerImageUrl = "https://images.unsplash.com/photo-1517649763962-0c623266ddc0?w=1200&q=80",
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync();

        // 3. Seed Categories
        var cat1 = new Category { EventId = event1.EventId, Name = "Full Marathon Open", Description = "Official 42.2km qualifier for age 20+", MinAge = 20, MaxAge = 80, DistanceKm = 42.2m, EntryFee = 350.00m };
        var cat2 = new Category { EventId = event1.EventId, Name = "Half Marathon 21km", Description = "Competitive 21.1km scenic road race open to athletes 16+", MinAge = 16, MaxAge = 80, DistanceKm = 21.1m, EntryFee = 250.00m };
        var cat3 = new Category { EventId = event1.EventId, Name = "Coastal 10km Challenge", Description = "Fast and flat 10km dash along the oceanfront", MinAge = 14, MaxAge = 80, DistanceKm = 10.0m, EntryFee = 160.00m };

        var cat4 = new Category { EventId = event2.EventId, Name = "Family Fun Walk 5km", Description = "All-ages leisurely coastal walk with finisher medals", MinAge = 5, MaxAge = 90, DistanceKm = 5.0m, EntryFee = 90.00m };
        var cat5 = new Category { EventId = event2.EventId, Name = "Senior Stride 3km", Description = "Gentle guided beachfront walk for seniors aged 55+", MinAge = 55, MaxAge = 95, DistanceKm = 3.0m, EntryFee = 60.00m };

        var cat6 = new Category { EventId = event3.EventId, Name = "Elite Classic 94.7km", Description = "Timed road race for licensed and seasoned cyclists", MinAge = 18, MaxAge = 70, DistanceKm = 94.7m, EntryFee = 480.00m };
        var cat7 = new Category { EventId = event3.EventId, Name = "Open Challenge 55km", Description = "Intermediate road ride across scenic undulating terrain", MinAge = 16, MaxAge = 75, DistanceKm = 55.0m, EntryFee = 320.00m };

        context.Categories.AddRange(cat1, cat2, cat3, cat4, cat5, cat6, cat7);
        await context.SaveChangesAsync();

        // 4. Seed Enrolments
        var enr1 = new Enrolment { ParticipantId = participant1.UserId, EventId = event1.EventId, CategoryId = cat2.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-10), Status = "Confirmed", BibNumber = 1042 };
        var enr2 = new Enrolment { ParticipantId = participant2.UserId, EventId = event1.EventId, CategoryId = cat1.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-8), Status = "Confirmed", BibNumber = 1043 };
        var enr3 = new Enrolment { ParticipantId = participant3.UserId, EventId = event1.EventId, CategoryId = cat3.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-5), Status = "Confirmed", BibNumber = 1044 };

        var enr4 = new Enrolment { ParticipantId = participant1.UserId, EventId = event2.EventId, CategoryId = cat4.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-4), Status = "Pending", BibNumber = 2081 };
        var enr5 = new Enrolment { ParticipantId = participant2.UserId, EventId = event2.EventId, CategoryId = cat4.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-3), Status = "Confirmed", BibNumber = 2082 };

        var enr6 = new Enrolment { ParticipantId = participant2.UserId, EventId = event3.EventId, CategoryId = cat6.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-45), Status = "Confirmed", BibNumber = 3015 };
        var enr7 = new Enrolment { ParticipantId = participant3.UserId, EventId = event3.EventId, CategoryId = cat6.CategoryId, EnrolmentDate = DateTime.UtcNow.AddDays(-44), Status = "Confirmed", BibNumber = 3016 };

        context.Enrolments.AddRange(enr1, enr2, enr3, enr4, enr5, enr6, enr7);
        await context.SaveChangesAsync();

        // 5. Seed Results for completed Event 3
        var res1 = new Result
        {
            EnrolmentId = enr7.EnrolmentId,
            ParticipantId = participant3.UserId,
            EventId = event3.EventId,
            FinishTime = "02:28:44",
            FinishSeconds = 8924,
            FinishingPosition = 12,
            TotalFinishers = 312,
            PublishedAt = DateTime.UtcNow.AddDays(-13),
            Notes = "Top 15 Finish - Outstanding Performance!"
        };

        var res2 = new Result
        {
            EnrolmentId = enr6.EnrolmentId,
            ParticipantId = participant2.UserId,
            EventId = event3.EventId,
            FinishTime = "02:47:19",
            FinishSeconds = 10039,
            FinishingPosition = 47,
            TotalFinishers = 312,
            PublishedAt = DateTime.UtcNow.AddDays(-13),
            Notes = "Personal Best - 47th out of 312 finishers"
        };

        context.Results.AddRange(res1, res2);
        await context.SaveChangesAsync();
    }
}
