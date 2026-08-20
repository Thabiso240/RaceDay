using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs;

public class CreateCategoryDto
{
    [Required]
    public int EventId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public decimal? DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; } = 0.00m;
}

public class UpdateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public decimal? DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int EnrolledCount { get; set; }
}
