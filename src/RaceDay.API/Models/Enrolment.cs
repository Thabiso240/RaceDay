using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Enrolment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrolmentId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public User? Participant { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Confirmed"; // "Confirmed", "Pending", "Cancelled"

    public int BibNumber { get; set; }

    // Navigation property
    public Result? Result { get; set; }
}
