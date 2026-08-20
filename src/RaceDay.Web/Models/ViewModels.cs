using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RaceDay.API.DTOs;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [Display(Name = "Full Name")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your primary role.")]
    [RegularExpression("^(Organiser|Participant)$")]
    public string Role { get; set; } = "Participant";

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
}

public class EventBrowseViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? SelectedType { get; set; }
    public string? SearchQuery { get; set; }
}

public class EventDetailViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public string? CurrentUserRole { get; set; }
    public bool IsEnrolled { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Race title is required.")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Detailed event description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Event date and start time are required.")]
    [Display(Name = "Event Date & Time")]
    public DateTime EventDate { get; set; } = DateTime.UtcNow.AddDays(14);

    [Required(ErrorMessage = "Location venue is required.")]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Race distance in km is required.")]
    [Range(0.1, 1000.0, ErrorMessage = "Distance must be between 0.1km and 1000km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 10.0m;

    [Required(ErrorMessage = "Please select an event type.")]
    [RegularExpression("^(Run|Walk|Cycle)$")]
    [Display(Name = "Sport Discipline")]
    public string EventType { get; set; } = "Run";

    public string? Status { get; set; } = "Upcoming";

    public string? CurrentBannerUrl { get; set; }

    [Display(Name = "Upload Banner Image (Azure Blob Storage)")]
    public IFormFile? BannerImage { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required (e.g. Under 20, Senior, 10km).")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Display(Name = "Minimum Age")]
    public int? MinAge { get; set; }

    [Display(Name = "Maximum Age")]
    public int? MaxAge { get; set; }

    [Display(Name = "Specific Distance (km, optional)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Entry fee is required.")]
    [Range(0, 50000, ErrorMessage = "Entry fee must be positive.")]
    [Display(Name = "Entry Fee (ZAR)")]
    public decimal EntryFee { get; set; } = 0.00m;
}

public class CaptureResultsViewModel
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public List<EnrolmentDto> EnrolledAthletes { get; set; } = new();
    public List<AthleteResultEntry> Entries { get; set; } = new();
}

public class AthleteResultEntry
{
    public int EnrolmentId { get; set; }
    public string AthleteName { get; set; } = string.Empty;
    public int BibNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = "00:00:00";
    public string? Notes { get; set; }
}

public class ProfileViewModel
{
    public int UserId { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "Upload New Profile Photo")]
    public IFormFile? ProfilePicture { get; set; }

    public int TotalActivities { get; set; }
}
