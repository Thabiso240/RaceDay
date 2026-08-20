using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionService _sessionService;

    public AuthController(
        RaceDayDbContext context,
        IPasswordHasher passwordHasher,
        ISessionService sessionService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Registers a new user as an Organiser or Participant.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (existingUser)
        {
            return Conflict(new { error = "Conflict", message = $"A user account with email '{dto.Email}' already exists." });
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            Role = dto.Role,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Automatically establish an authenticated session upon registration
        var session = await _sessionService.CreateSessionAsync(user.UserId, user.Role);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl,
            SessionToken = session.SessionToken,
            ExpiresAt = session.ExpiresAt
        };

        return CreatedAtAction(nameof(GetMe), new { }, response);
    }

    /// <summary>
    /// Authenticates user credentials and establishes a session.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Unauthorized", message = "Invalid email or password credentials." });
        }

        var session = await _sessionService.CreateSessionAsync(user.UserId, user.Role);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl,
            SessionToken = session.SessionToken,
            ExpiresAt = session.ExpiresAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Logs out the user and invalidates the session.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = User.FindFirst("SessionToken")?.Value;
        if (string.IsNullOrEmpty(sessionToken) && Request.Headers.TryGetValue("X-Session-Token", out var headerToken))
        {
            sessionToken = headerToken.ToString();
        }

        if (!string.IsNullOrEmpty(sessionToken))
        {
            await _sessionService.InvalidateSessionAsync(sessionToken);
        }

        return Ok(new { message = "Logged out successfully. Session invalidated." });
    }

    /// <summary>
    /// Returns the currently authenticated user's profile and active session role.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userIdStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Unauthorized", message = "No valid authenticated session found." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized(new { error = "Unauthorized", message = "User not found." });

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
}
