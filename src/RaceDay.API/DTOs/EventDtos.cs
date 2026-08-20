using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CreateEventDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 1000.0, ErrorMessage = "Distance must be between 0.1km and 1000km.")]
    public decimal DistanceKm { get; set; }

    [Required]
    [RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "Event type must be 'Run', 'Walk', or 'Cycle'.")]
    public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }
}

public class UpdateEventDto
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(0.1, 1000.0)]
    public decimal DistanceKm { get; set; }

    [Required]
    [RegularExpression("^(Run|Walk|Cycle)$")]
    public string EventType { get; set; } = "Run";

    [RegularExpression("^(Upcoming|InProgress|Completed|Cancelled)$")]
    public string Status { get; set; } = "Upcoming";

    public string? BannerImageUrl { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
    public string Status { get; set; } = "Upcoming";
    public int TotalEnrolments { get; set; }
    public int CategoryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}
