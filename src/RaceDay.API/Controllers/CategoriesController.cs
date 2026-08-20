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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all categories defined for a specific event.
    /// </summary>
    [HttpGet("event/{eventId}")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var categories = await _context.Categories
            .Include(c => c.Enrolments)
            .Where(c => c.EventId == eventId)
            .AsNoTracking()
            .Select(c => new CategoryDto
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
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Adds a category to an event (Organisers only).
    /// </summary>
    [HttpPost]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var raceEvent = await _context.Events.FindAsync(dto.EventId);
        if (raceEvent == null)
            return NotFound(new { error = "Not Found", message = $"Event {dto.EventId} does not exist." });

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You can only add categories to events you organise." });
        }

        var category = new Category
        {
            EventId = dto.EventId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var resultDto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            EnrolledCount = 0
        };

        return CreatedAtAction(nameof(GetByEvent), new { eventId = category.EventId }, resultDto);
    }

    /// <summary>
    /// Updates category specifications (Organisers only).
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound();

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (category.Event?.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "Forbidden", message = "You cannot modify categories for another organiser's event." });
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.MinAge = dto.MinAge;
        category.MaxAge = dto.MaxAge;
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee
        });
    }

    /// <summary>
    /// Deletes a category (Organisers only).
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeRole("Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound();

        var organiserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (category.Event?.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (category.Enrolments.Any())
        {
            return Conflict(new { error = "Conflict", message = "Cannot delete category as athletes have already enrolled in it." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
