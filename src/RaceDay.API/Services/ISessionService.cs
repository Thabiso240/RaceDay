using RaceDay.API.Models;

namespace RaceDay.API.Services;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(int userId, string role, TimeSpan? duration = null);
    Task<UserSession?> ValidateSessionAsync(string sessionToken);
    Task<bool> InvalidateSessionAsync(string sessionToken);
}
