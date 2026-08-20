using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Event
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventId { get; set; }

    [Required]
    public int OrganiserId { get; set; }

    [ForeignKey(nameof(OrganiserId))]
    public User? Organiser { get; set; }

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
    [Column(TypeName = "decimal(5,2)")]
    public decimal DistanceKm { get; set; }

    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = "Run"; // "Run", "Walk", "Cycle"

    [MaxLength(500)]
    public string? BannerImageUrl { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Upcoming"; // "Upcoming", "InProgress", "Completed", "Cancelled"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<Result> Results { get; set; } = new List<Result>();
}
