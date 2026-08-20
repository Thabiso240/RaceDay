using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Security;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobService;

    public EventsController(RaceDayDbContext context, IBlobStorageService blobService)
    {
        _context = context;
        _blobService = blobService;
    }

    /// <summary>
    /// Retrieves all events with optional filters for event type (Run, Walk, Cycle) and search keyword.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] string? type, [FromQuery] string? search)
    {
        var query = _context.Events
            .Include(e => e.Organiser)
            .Include(e => e.Enrolments)
            .Include(e => e.Categories)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(e => e.EventType.ToLower() == type.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(s) || e.Location.ToLower().Contains(s) || e.Description.ToLower().Contains(s));
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser != null ? e.Organiser.FullName : "Organiser",
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
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Retrieves full event details, including competition categories.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById(int id)
    {
        var e = await _context.Events
            .Include(ev => ev.Organiser)
            .Include(ev => ev.Enrolments)
            .Include(ev => ev.Categories)
            .ThenInclude(c => c.Enrolments)
            .AsNoTracking()
            .FirstOrDefaultAsync(ev => ev.EventId == id);

        if (e == null)
            return NotFound(new { error = "Not Found", message = $"Event with ID {id} was not found." });

        var dto = new EventDetailDto
        {
            EventId = e.EventId,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser != null ? e.Organiser.FullName : "Organiser",
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
            CreatedAt = e.CreatedAt,
            Categories = e.Categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Description = c.Description,
                MinAge = c.MinAge,
                MaxAge = c.MaxAge,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                EnrolledCount = c.Enrolments.Count
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new race event (Organisers only).
    /// </summary>
    [HttpPost]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var raceEvent = new Event
        {
            OrganiserId = organiserId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            BannerImageUrl = dto.BannerImageUrl,
            Status = "Upcoming",
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        var resultDto = new EventDto
        {
            EventId = raceEvent.EventId,
            OrganiserId = raceEvent.OrganiserId,
            OrganiserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Organiser",
            Title = raceEvent.Title,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            EventType = raceEvent.EventType,
            BannerImageUrl = raceEvent.BannerImageUrl,
            Status = raceEvent.Status,
            TotalEnrolments = 0,
            CategoryCount = 0,
            CreatedAt = raceEvent.CreatedAt
        };

        return CreatedAtAction(nameof(GetEventById), new { id = raceEvent.EventId }, resultDto);
    }

    /// <summary>
    /// Updates event details (Organisers only).
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var raceEvent = await _context.Events.FindAsync(id);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event with ID {id} does not exist." });

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only edit events created by your own organiser account." });
        }

        raceEvent.Title = dto.Title.Trim();
        raceEvent.Description = dto.Description.Trim();
        raceEvent.EventDate = dto.EventDate;
        raceEvent.Location = dto.Location.Trim();
        raceEvent.DistanceKm = dto.DistanceKm;
        raceEvent.EventType = dto.EventType;
        raceEvent.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.BannerImageUrl))
        {
            raceEvent.BannerImageUrl = dto.BannerImageUrl;
        }

        await _context.SaveChangesAsync();

        return Ok(new EventDto
        {
            EventId = raceEvent.EventId,
            OrganiserId = raceEvent.OrganiserId,
            OrganiserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Organiser",
            Title = raceEvent.Title,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            EventType = raceEvent.EventType,
            BannerImageUrl = raceEvent.BannerImageUrl,
            Status = raceEvent.Status,
            CreatedAt = raceEvent.CreatedAt
        });
    }

    /// <summary>
    /// Deletes an event (Organisers only).
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await _context.Events
            .Include(e => e.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null)
            return NotFound();

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only delete your own events." });
        }

        if (raceEvent.Enrolments.Any(e => e.Status == "Confirmed"))
        {
            return Conflict(new { error = "Conflict", message = "Cannot delete this event because active participant enrolments are registered." });
        }

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Uploads an event banner image directly to Azure Blob Storage (Organisers only).
    /// </summary>
    [HttpPost("{id}/banner")]
    [AuthorizeRole("Organiser")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, [FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Bad Request", message = "Please supply a valid image file for the event banner." });
        }

        var raceEvent = await _context.Events.FindAsync(id);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event {id} not found." });

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only upload banners for events you manage." });
        }

        using var stream = file.OpenReadStream();
        var bannerUrl = await _blobService.UploadFileAsync(stream, file.FileName, "banners", file.ContentType);

        raceEvent.BannerImageUrl = bannerUrl;
        await _context.SaveChangesAsync();

        return Ok(new { bannerImageUrl = bannerUrl, message = "Event banner successfully uploaded to Azure Blob Storage." });
    }
}
