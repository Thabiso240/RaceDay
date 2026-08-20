using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CaptureResultItemDto
{
    [Required]
    public int EnrolmentId { get; set; }

    [Required]
    [RegularExpression(@"^([0-9]{1,2}:)?[0-5]?[0-9]:[0-5][0-9]$", ErrorMessage = "Finish time must be in HH:mm:ss or mm:ss format.")]
    public string FinishTime { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

public class CaptureResultsBatchDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one result item is required.")]
    public List<CaptureResultItemDto> Results { get; set; } = new();
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int BibNumber { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int FinishSeconds { get; set; }
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }
    public string FormattedPosition => $"{FinishingPosition} of {TotalFinishers}";
    public DateTime PublishedAt { get; set; }
    public string? Notes { get; set; }
}

public class PersonalResultDto
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int BibNumber { get; set; }
    public string FinishTime { get; set; } = string.Empty;
    public int FinishingPosition { get; set; }
    public int TotalFinishers { get; set; }
    public string PositionDisplay => $"{GetOrdinal(FinishingPosition)} out of {TotalFinishers} finishers";
    public DateTime PublishedAt { get; set; }
    public string? Notes { get; set; }

    private static string GetOrdinal(int number)
    {
        if (number <= 0) return number.ToString();
        switch (number % 100)
        {
            case 11:
            case 12:
            case 13:
                return $"{number}th";
        }
        switch (number % 10)
        {
            case 1: return $"{number}st";
            case 2: return $"{number}nd";
            case 3: return $"{number}rd";
            default: return $"{number}th";
        }
    }
}
