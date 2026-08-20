using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CreateEnrolmentDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

public class UpdateEnrolmentStatusDto
{
    [Required]
    [RegularExpression("^(Confirmed|Pending|Cancelled)$", ErrorMessage = "Status must be 'Confirmed', 'Pending', or 'Cancelled'.")]
    public string Status { get; set; } = "Confirmed";
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = "Confirmed";
    public int BibNumber { get; set; }
    public bool HasResult { get; set; }
    public string? FinishTime { get; set; }
    public int? FinishingPosition { get; set; }
}
