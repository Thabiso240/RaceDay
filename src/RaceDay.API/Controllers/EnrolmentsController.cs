using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enrols the authenticated Participant in an event under a selected category.
    /// </summary>
    [HttpPost]
    [AuthorizeRole("Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var participantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verify Event
        var raceEvent = await _context.Events.FindAsync(dto.EventId);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event {dto.EventId} does not exist." });

        if (raceEvent.Status == "Completed" || raceEvent.Status == "Cancelled")
            return BadRequest(new { error = "Bad Request", message = $"Registration is closed. Event is currently {raceEvent.Status}." });

        // Verify Category
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == dto.EventId);
        if (category == null)
            return BadRequest(new { error = "Bad Request", message = $"Category {dto.CategoryId} is not valid for Event {dto.EventId}." });

        // Verify no duplicate enrolment
        var alreadyEnrolled = await _context.Enrolments.AnyAsync(e => e.ParticipantId == participantId && e.EventId == dto.EventId);
        if (alreadyEnrolled)
        {
            return Conflict(new { error = "Conflict", message = "You have already entered this event. Multiple entries per athlete are not permitted." });
        }

        // Generate Bib Number (highest current bib + 1, starting at 1001)
        var maxBib = await _context.Enrolments
            .Where(e => e.EventId == dto.EventId)
            .Select(e => (int?)e.BibNumber)
            .MaxAsync() ?? 1000;

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = dto.EventId,
            CategoryId = dto.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed",
            BibNumber = maxBib + 1
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var participant = await _context.Users.FindAsync(participantId);

        var resultDto = new EnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = participant?.FullName ?? "Athlete",
            ParticipantEmail = participant?.Email ?? "",
            EventId = raceEvent.EventId,
            EventTitle = raceEvent.Title,
            EventDate = raceEvent.EventDate,
            EventType = raceEvent.EventType,
            BannerImageUrl = raceEvent.BannerImageUrl,
            CategoryId = category.CategoryId,
            CategoryName = category.Name,
            EntryFee = category.EntryFee,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status,
            BibNumber = enrolment.BibNumber,
            HasResult = false
        };

        return CreatedAtAction(nameof(GetMyEnrolments), new { }, resultDto);
    }

    /// <summary>
    /// Retrieves all enrolments for the authenticated Participant.
    /// </summary>
    [HttpGet("my")]
    [AuthorizeRole("Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var enrolments = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Result)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .AsNoTracking()
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = User.FindFirst(ClaimTypes.Name)!.Value,
                ParticipantEmail = User.FindFirst(ClaimTypes.Email)!.Value,
                EventId = e.EventId,
                EventTitle = e.Event != null ? e.Event.Title : "Race Event",
                EventDate = e.Event != null ? e.Event.EventDate : DateTime.UtcNow,
                EventType = e.Event != null ? e.Event.EventType : "Run",
                BannerImageUrl = e.Event != null ? e.Event.BannerImageUrl : null,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : "Open",
                EntryFee = e.Category != null ? e.Category.EntryFee : 0m,
                EnrolmentDate = e.EnrolmentDate,
                Status = e.Status,
                BibNumber = e.BibNumber,
                HasResult = e.Result != null,
                FinishTime = e.Result != null ? e.Result.FinishTime : null,
                FinishingPosition = e.Result != null ? e.Result.FinishingPosition : null
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// Retrieves all athlete enrolments for an event (Organisers only).
    /// </summary>
    [HttpGet("event/{eventId}")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var raceEvent = await _context.Events.FindAsync(eventId);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event {eventId} not found." });

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only view participant enrolments for your own events." });
        }

        var list = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Include(e => e.Result)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.BibNumber)
            .AsNoTracking()
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant != null ? e.Participant.FullName : "Athlete",
                ParticipantEmail = e.Participant != null ? e.Participant.Email : "",
                EventId = e.EventId,
                EventTitle = raceEvent.Title,
                EventDate = raceEvent.EventDate,
                EventType = raceEvent.EventType,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : "",
                EntryFee = e.Category != null ? e.Category.EntryFee : 0m,
                EnrolmentDate = e.EnrolmentDate,
                Status = e.Status,
                BibNumber = e.BibNumber,
                HasResult = e.Result != null,
                FinishTime = e.Result != null ? e.Result.FinishTime : null,
                FinishingPosition = e.Result != null ? e.Result.FinishingPosition : null
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Updates enrolment status (e.g. from Pending to Confirmed or Cancelled) (Organisers only).
    /// </summary>
    [HttpPut("{id}/status")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound();

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (enrolment.Event?.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only update statuses for your own events." });
        }

        enrolment.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new EnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant?.FullName ?? "",
            ParticipantEmail = enrolment.Participant?.Email ?? "",
            EventId = enrolment.EventId,
            EventTitle = enrolment.Event?.Title ?? "",
            EventDate = enrolment.Event?.EventDate ?? DateTime.UtcNow,
            EventType = enrolment.Event?.EventType ?? "",
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category?.Name ?? "",
            EntryFee = enrolment.Category?.EntryFee ?? 0,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status,
            BibNumber = enrolment.BibNumber
        });
    }
}
