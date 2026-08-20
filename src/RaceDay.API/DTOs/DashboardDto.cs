namespace RaceDay.API.DTOs;

public class OrganiserDashboardDto
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int UpcomingEventsCount { get; set; }
    public int CompletedEventsCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<EventDto> UpcomingEvents { get; set; } = new();
    public List<EventDto> RecentEvents { get; set; } = new();
}
