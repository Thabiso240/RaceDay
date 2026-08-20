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
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures and publishes finish times and positions for participants after an event concludes (Organisers only).
    /// </summary>
    [HttpPost("capture")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CaptureResults([FromBody] CaptureResultsBatchDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var raceEvent = await _context.Events.FindAsync(dto.EventId);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event {dto.EventId} does not exist." });

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only capture results for events you organise." });
        }

        // Parse and calculate seconds for each submitted finish time to sort properly
        var parsedList = new List<(CaptureResultItemDto Item, int Seconds)>();
        foreach (var item in dto.Results)
        {
            var seconds = ParseTimeToSeconds(item.FinishTime);
            if (seconds <= 0)
            {
                return BadRequest(new { error = "Bad Request", message = $"Invalid finish time '{item.FinishTime}' for enrolment {item.EnrolmentId}. Use format hh:mm:ss or mm:ss." });
            }
            parsedList.Add((item, seconds));
        }

        // Sort ascending by finish time (fastest finisher is 1st place)
        parsedList = parsedList.OrderBy(p => p.Seconds).ToList();
        var totalFinishers = parsedList.Count;

        var resultsToReturn = new List<ResultDto>();
        int position = 1;

        foreach (var (item, seconds) in parsedList)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EnrolmentId == item.EnrolmentId && e.EventId == dto.EventId);

            if (enrolment == null)
                continue;

            // Check if existing result exists to update, or create new
            var existingResult = await _context.Results.FirstOrDefaultAsync(r => r.EnrolmentId == item.EnrolmentId);
            if (existingResult != null)
            {
                existingResult.FinishTime = item.FinishTime;
                existingResult.FinishSeconds = seconds;
                existingResult.FinishingPosition = position;
                existingResult.TotalFinishers = totalFinishers;
                existingResult.Notes = item.Notes;
                existingResult.PublishedAt = DateTime.UtcNow;
            }
            else
            {
                var newResult = new Result
                {
                    EnrolmentId = enrolment.EnrolmentId,
                    ParticipantId = enrolment.ParticipantId,
                    EventId = dto.EventId,
                    FinishTime = item.FinishTime,
                    FinishSeconds = seconds,
                    FinishingPosition = position,
                    TotalFinishers = totalFinishers,
                    Notes = item.Notes,
                    PublishedAt = DateTime.UtcNow
                };
                _context.Results.Add(newResult);
            }

            resultsToReturn.Add(new ResultDto
            {
                EnrolmentId = enrolment.EnrolmentId,
                ParticipantId = enrolment.ParticipantId,
                ParticipantName = enrolment.Participant?.FullName ?? "Finisher",
                BibNumber = enrolment.BibNumber,
                EventId = dto.EventId,
                EventTitle = raceEvent.Title,
                CategoryName = enrolment.Category?.Name ?? "Open",
                FinishTime = item.FinishTime,
                FinishSeconds = seconds,
                FinishingPosition = position,
                TotalFinishers = totalFinishers,
                Notes = item.Notes,
                PublishedAt = DateTime.UtcNow
            });

            position++;
        }

        raceEvent.Status = "Completed";
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEventResults), new { eventId = dto.EventId }, resultsToReturn);
    }

    /// <summary>
    /// Gets public race results and leaderboard for a specific event.
    /// </summary>
    [HttpGet("event/{eventId}")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var results = await _context.Results
            .Include(r => r.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
            .Include(r => r.Event)
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.FinishingPosition)
            .AsNoTracking()
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantId = r.ParticipantId,
                ParticipantName = r.Participant != null ? r.Participant.FullName : "Finisher",
                BibNumber = r.Enrolment != null ? r.Enrolment.BibNumber : 0,
                EventId = r.EventId,
                EventTitle = r.Event != null ? r.Event.Title : "",
                CategoryName = r.Enrolment != null && r.Enrolment.Category != null ? r.Enrolment.Category.Name : "",
                FinishTime = r.FinishTime,
                FinishSeconds = r.FinishSeconds,
                FinishingPosition = r.FinishingPosition,
                TotalFinishers = r.TotalFinishers,
                PublishedAt = r.PublishedAt,
                Notes = r.Notes
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Retrieves the personal race history and verified results for the logged-in Participant.
    /// </summary>
    [HttpGet("my")]
    [AuthorizeRole("Participant")]
    [ProducesResponseType(typeof(IEnumerable<PersonalResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var list = await _context.Results
            .Include(r => r.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
            .Where(r => r.ParticipantId == participantId)
            .OrderByDescending(r => r.PublishedAt)
            .AsNoTracking()
            .Select(r => new PersonalResultDto
            {
                ResultId = r.ResultId,
                EventId = r.EventId,
                EventTitle = r.Event != null ? r.Event.Title : "Race",
                EventDate = r.Event != null ? r.Event.EventDate : DateTime.UtcNow,
                EventType = r.Event != null ? r.Event.EventType : "Run",
                CategoryName = r.Enrolment != null && r.Enrolment.Category != null ? r.Enrolment.Category.Name : "Open",
                DistanceKm = r.Event != null ? r.Event.DistanceKm : 0m,
                BibNumber = r.Enrolment != null ? r.Enrolment.BibNumber : 0,
                FinishTime = r.FinishTime,
                FinishingPosition = r.FinishingPosition,
                TotalFinishers = r.TotalFinishers,
                PublishedAt = r.PublishedAt,
                Notes = r.Notes
            })
            .ToListAsync();

        return Ok(list);
    }

    private static int ParseTimeToSeconds(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
            return 0;

        var parts = timeStr.Trim().Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var s))
        {
            return m * 60 + s;
        }
        if (parts.Length == 3 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var min) && int.TryParse(parts[2], out var sec))
        {
            return h * 3600 + min * 60 + sec;
        }

        return 0;
    }
}
