using Microsoft.AspNetCore.Mvc;
using RaceDay.API.DTOs;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : Controller
{
    private readonly IRaceDayApiClient _apiClient;

    public OrganiserController(IRaceDayApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    private string? EnsureOrganiser()
    {
        var token = HttpContext.Session.GetString("SessionToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrEmpty(token) || role != "Organiser")
            return null;

        return token;
    }

    // 1. Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var token = EnsureOrganiser();
        if (token == null)
        {
            TempData["Error"] = "Access restricted. Please log in with an Organiser account.";
            return RedirectToAction("Login", "Account", new { returnUrl = "/Organiser/Dashboard" });
        }

        var dashboard = await _apiClient.GetOrganiserDashboardAsync(token);
        return View(dashboard ?? new OrganiserDashboardDto());
    }

    // 2. Events Management List
    public async Task<IActionResult> Events()
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var dashboard = await _apiClient.GetOrganiserDashboardAsync(token);
        var events = dashboard?.UpcomingEvents.Concat(dashboard?.RecentEvents ?? new()).DistinctBy(e => e.EventId).ToList() ?? new();
        return View(events);
    }

    // 3. Create Event
    [HttpGet]
    public IActionResult CreateEvent()
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var created = await _apiClient.CreateEventAsync(token, new CreateEventDto
        {
            Title = model.Title,
            Description = model.Description,
            EventDate = model.EventDate,
            Location = model.Location,
            DistanceKm = model.DistanceKm,
            EventType = model.EventType
        });

        if (created == null)
        {
            ModelState.AddModelError(string.Empty, "Could not create event. Please ensure all values are valid.");
            return View(model);
        }

        // Upload banner image to Azure Blob Storage via API if supplied
        if (model.BannerImage != null && model.BannerImage.Length > 0)
        {
            using var stream = model.BannerImage.OpenReadStream();
            await _apiClient.UploadEventBannerAsync(
                token, created.EventId, stream, model.BannerImage.FileName, model.BannerImage.ContentType);
        }

        TempData["Success"] = $"Event '{created.Title}' created successfully! Now add categories for your participants.";
        return RedirectToAction("Categories", new { eventId = created.EventId });
    }

    // 4. Edit Event
    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var ev = await _apiClient.GetEventByIdAsync(id);
        if (ev == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var vm = new EventFormViewModel
        {
            EventId = ev.EventId,
            Title = ev.Title,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            Status = ev.Status,
            CurrentBannerUrl = ev.BannerImageUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
            return View(model);

        var updated = await _apiClient.UpdateEventAsync(token, id, new UpdateEventDto
        {
            Title = model.Title,
            Description = model.Description,
            EventDate = model.EventDate,
            Location = model.Location,
            DistanceKm = model.DistanceKm,
            EventType = model.EventType,
            Status = model.Status ?? "Upcoming",
            BannerImageUrl = model.CurrentBannerUrl
        });

        if (updated == null)
        {
            ModelState.AddModelError(string.Empty, "Failed to update event.");
            return View(model);
        }

        // Handle banner update if new file uploaded
        if (model.BannerImage != null && model.BannerImage.Length > 0)
        {
            using var stream = model.BannerImage.OpenReadStream();
            await _apiClient.UploadEventBannerAsync(
                token, id, stream, model.BannerImage.FileName, model.BannerImage.ContentType);
        }

        TempData["Success"] = "Event updated successfully!";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var ok = await _apiClient.DeleteEventAsync(token, id);
        if (!ok)
        {
            TempData["Error"] = "Cannot delete this event. Events with confirmed participant registrations cannot be removed.";
        }
        else
        {
            TempData["Success"] = "Event deleted successfully.";
        }

        return RedirectToAction(nameof(Events));
    }

    // 5. Categories Management
    public async Task<IActionResult> Categories(int eventId)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var raceEvent = await _apiClient.GetEventByIdAsync(eventId);
        if (raceEvent == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var categories = await _apiClient.GetCategoriesByEventAsync(eventId);
        ViewBag.Event = raceEvent;
        ViewBag.Categories = categories;

        var vm = new CategoryFormViewModel
        {
            EventId = eventId,
            EventTitle = raceEvent.Title
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please provide valid category details.";
            return RedirectToAction("Categories", new { eventId = model.EventId });
        }

        var cat = await _apiClient.CreateCategoryAsync(token, new CreateCategoryDto
        {
            EventId = model.EventId,
            Name = model.Name,
            Description = model.Description,
            MinAge = model.MinAge,
            MaxAge = model.MaxAge,
            DistanceKm = model.DistanceKm,
            EntryFee = model.EntryFee
        });

        if (cat == null)
        {
            TempData["Error"] = "Could not create category.";
        }
        else
        {
            TempData["Success"] = $"Category '{cat.Name}' added successfully!";
        }

        return RedirectToAction("Categories", new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var ok = await _apiClient.DeleteCategoryAsync(token, id);
        if (!ok)
        {
            TempData["Error"] = "Cannot delete this category because participants have already enrolled in it.";
        }
        else
        {
            TempData["Success"] = "Category removed successfully.";
        }

        return RedirectToAction("Categories", new { eventId });
    }

    // 6. Enrolments Page (View all athletes for specific event)
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var raceEvent = await _apiClient.GetEventByIdAsync(eventId);
        if (raceEvent == null) return RedirectToAction(nameof(Events));

        var enrolments = await _apiClient.GetEventEnrolmentsAsync(token, eventId);
        ViewBag.Event = raceEvent;

        return View(enrolments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, int eventId, string status)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var ok = await _apiClient.UpdateEnrolmentStatusAsync(token, id, status);
        if (ok)
        {
            TempData["Success"] = $"Athlete status changed to {status}.";
        }
        else
        {
            TempData["Error"] = "Could not update athlete status.";
        }

        return RedirectToAction("Enrolments", new { eventId });
    }

    // 7. Results Entry Page (Capture finish times & positions)
    [HttpGet]
    public async Task<IActionResult> CaptureResults(int eventId)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var raceEvent = await _apiClient.GetEventByIdAsync(eventId);
        if (raceEvent == null) return RedirectToAction(nameof(Events));

        var enrolments = await _apiClient.GetEventEnrolmentsAsync(token, eventId);
        var confirmedEnrolments = enrolments.Where(e => e.Status == "Confirmed").ToList();

        var vm = new CaptureResultsViewModel
        {
            EventId = eventId,
            EventTitle = raceEvent.Title,
            EventDate = raceEvent.EventDate,
            EnrolledAthletes = confirmedEnrolments,
            Entries = confirmedEnrolments.Select(e => new AthleteResultEntry
            {
                EnrolmentId = e.EnrolmentId,
                AthleteName = e.ParticipantName,
                BibNumber = e.BibNumber,
                CategoryName = e.CategoryName,
                FinishTime = e.FinishTime ?? "00:00:00"
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CaptureResults(CaptureResultsViewModel model)
    {
        var token = EnsureOrganiser();
        if (token == null) return RedirectToAction("Login", "Account");

        var batch = new CaptureResultsBatchDto
        {
            EventId = model.EventId,
            Results = model.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.FinishTime) && e.FinishTime != "00:00:00")
                .Select(e => new CaptureResultItemDto
                {
                    EnrolmentId = e.EnrolmentId,
                    FinishTime = e.FinishTime.Trim(),
                    Notes = e.Notes
                }).ToList()
        };

        if (batch.Results.Count == 0)
        {
            TempData["Error"] = "Please enter at least one valid finish time (hh:mm:ss).";
            return RedirectToAction(nameof(CaptureResults), new { eventId = model.EventId });
        }

        var results = await _apiClient.CaptureResultsAsync(token, batch);
        if (results == null)
        {
            TempData["Error"] = "Failed to publish race results. Please ensure all finish times are in valid hh:mm:ss format.";
            return RedirectToAction(nameof(CaptureResults), new { eventId = model.EventId });
        }

        TempData["Success"] = $"Results successfully published for {results.Count} finishers! Race leaderboards updated.";
        return RedirectToAction("Details", "Events", new { id = model.EventId });
    }
}
