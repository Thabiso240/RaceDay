using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; }

    [Required]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g. "Under 20", "Senior", "10km", "21km"

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DistanceKm { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; } = 0.00m;

    // Navigation properties
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
