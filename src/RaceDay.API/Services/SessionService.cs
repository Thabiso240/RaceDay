using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

public class SessionService : ISessionService
{
    private readonly RaceDayDbContext _context;

    public SessionService(RaceDayDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession> CreateSessionAsync(int userId, string role, TimeSpan? duration = null)
    {
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var sessionDuration = duration ?? TimeSpan.FromHours(24);

        var session = new UserSession
        {
            UserId = userId,
            Role = role,
            SessionToken = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(sessionDuration),
            IsActive = true
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<UserSession?> ValidateSessionAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return null;

        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

        return session;
    }

    public async Task<bool> InvalidateSessionAsync(string sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return false;

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive);

        if (session == null)
            return false;

        session.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
