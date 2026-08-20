using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrganiserController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public OrganiserController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves executive dashboard statistics for the logged-in Organiser.
    /// </summary>
    [HttpGet("dashboard")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(OrganiserDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard()
    {
        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var events = await _context.Events
            .Include(e => e.Enrolments)
                .ThenInclude(en => en.Category)
            .Include(e => e.Categories)
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .AsNoTracking()
            .ToListAsync();

        var totalEvents = events.Count;
        var totalEnrolments = events.Sum(e => e.Enrolments.Count);
        var upcomingEvents = events.Where(e => e.EventDate >= DateTime.UtcNow && e.Status != "Cancelled").ToList();
        var completedEvents = events.Where(e => e.Status == "Completed" || e.EventDate < DateTime.UtcNow).ToList();

        var totalRevenue = events
            .SelectMany(e => e.Enrolments)
            .Where(en => en.Status == "Confirmed" && en.Category != null)
            .Sum(en => en.Category!.EntryFee);

        var upcomingDtos = upcomingEvents.Take(5).Select(e => new EventDto
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            Title = e.Title,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            BannerImageUrl = e.BannerImageUrl,
            Status = e.Status,
            TotalEnrolments = e.Enrolments.Count,
            CategoryCount = e.Categories.Count,
            CreatedAt = e.CreatedAt
        }).ToList();

        var recentDtos = events.OrderByDescending(e => e.CreatedAt).Take(5).Select(e => new EventDto
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            Title = e.Title,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            BannerImageUrl = e.BannerImageUrl,
            Status = e.Status,
            TotalEnrolments = e.Enrolments.Count,
            CategoryCount = e.Categories.Count,
            CreatedAt = e.CreatedAt
        }).ToList();

        return Ok(new OrganiserDashboardDto
        {
            TotalEvents = totalEvents,
            TotalEnrolments = totalEnrolments,
            UpcomingEventsCount = upcomingEvents.Count,
            CompletedEventsCount = completedEvents.Count,
            TotalRevenue = totalRevenue,
            UpcomingEvents = upcomingDtos,
            RecentEvents = recentDtos
        });
    }
}
