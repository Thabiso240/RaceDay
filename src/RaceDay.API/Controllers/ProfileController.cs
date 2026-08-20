using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Security;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobService;

    public ProfileController(RaceDayDbContext context, IBlobStorageService blobService)
    {
        _context = context;
        _blobService = blobService;
    }

    /// <summary>
    /// Retrieves user profile details.
    /// </summary>
    [HttpGet]
    [AuthorizeRole]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users
            .Include(u => u.OrganisedEvents)
            .Include(u => u.Enrolments)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { error = "Not Found", message = "User profile could not be found." });

        var total = user.Role == "Organiser" ? user.OrganisedEvents.Count : user.Enrolments.Count;

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            TotalEventsOrEnrolments = total
        });
    }

    /// <summary>
    /// Updates user personal details.
    /// </summary>
    [HttpPut]
    [AuthorizeRole]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Uploads a profile picture to Azure Blob Storage and attaches it to the user.
    /// </summary>
    [HttpPost("picture")]
    [AuthorizeRole]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Bad Request", message = "Please select a valid image file to upload." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = "Bad Request", message = "Invalid file type. Only JPG, PNG, and WebP images are permitted." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        using var stream = file.OpenReadStream();
        var imageUrl = await _blobService.UploadFileAsync(stream, file.FileName, "profiles", file.ContentType);

        user.ProfilePictureUrl = imageUrl;
        await _context.SaveChangesAsync();

        return Ok(new { profilePictureUrl = imageUrl, message = "Profile picture updated successfully via Azure Blob Storage." });
    }
}
