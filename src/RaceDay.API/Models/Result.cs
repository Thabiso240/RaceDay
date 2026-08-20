using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Result
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ResultId { get; set; }

    [Required]
    public int EnrolmentId { get; set; }

    [ForeignKey(nameof(EnrolmentId))]
    public Enrolment? Enrolment { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public User? Participant { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(20)]
    public string FinishTime { get; set; } = string.Empty; // e.g. "01:24:35"

    public int FinishSeconds { get; set; }

    public int FinishingPosition { get; set; } // e.g. 47

    public int TotalFinishers { get; set; } // e.g. 312

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(250)]
    public string? Notes { get; set; }
}
