using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.Tests;

public static class TestDatabaseFixture
{
    public static RaceDayDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new RaceDayDbContext(options);
    }

    public static ControllerContext CreateControllerContext(int userId, string role, string fullName = "Test User", string email = "test@raceday.co.za", string token = "sample-token")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("SessionToken", token)
        };

        var identity = new ClaimsIdentity(claims, "TestSessionAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    public static async Task<(User Organiser, Event Event, Category Category)> SeedOrganiserAndEventAsync(RaceDayDbContext context)
    {
        var hasher = new PasswordHasher();
        var organiser = new User
        {
            FullName = "Jarred Master Organiser",
            Email = $"organiser_{Guid.NewGuid():N}@raceday.co.za",
            PasswordHash = hasher.HashPassword("Password123!"),
            Role = "Organiser",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(organiser);
        await context.SaveChangesAsync();

        var raceEvent = new Event
        {
            OrganiserId = organiser.UserId,
            Title = "Jarred Cup Grand Invitational 2026",
            Description = "The prestigious athletic test of speed and stamina.",
            EventDate = DateTime.UtcNow.AddDays(14),
            Location = "Durban Stadium Track",
            DistanceKm = 21.10m,
            EventType = "Run",
            Status = "Upcoming",
            CreatedAt = DateTime.UtcNow
        };
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var category = new Category
        {
            EventId = raceEvent.EventId,
            Name = "Open Half Marathon",
            Description = "All competitors 18+",
            MinAge = 18,
            MaxAge = 75,
            DistanceKm = 21.10m,
            EntryFee = 220.00m
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return (organiser, raceEvent, category);
    }

    public static async Task<User> SeedParticipantAsync(RaceDayDbContext context, string name = "Jarred Dedicated Athlete")
    {
        var hasher = new PasswordHasher();
        var participant = new User
        {
            FullName = name,
            Email = $"athlete_{Guid.NewGuid():N}@raceday.co.za",
            PasswordHash = hasher.HashPassword("Password123!"),
            Role = "Participant",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(participant);
        await context.SaveChangesAsync();
        return participant;
    }
}
