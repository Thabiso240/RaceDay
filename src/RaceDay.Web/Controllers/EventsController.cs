using Microsoft.AspNetCore.Mvc;
using RaceDay.API.DTOs;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly IRaceDayApiClient _apiClient;

    public EventsController(IRaceDayApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var events = await _apiClient.GetEventsAsync(type, search);
        var vm = new EventBrowseViewModel
        {
            Events = events,
            SelectedType = type,
            SearchQuery = search
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var eventDetail = await _apiClient.GetEventByIdAsync(id);
        if (eventDetail == null)
        {
            TempData["Error"] = "The requested race event could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var token = HttpContext.Session.GetString("SessionToken");
        var role = HttpContext.Session.GetString("UserRole");

        bool isEnrolled = false;
        if (!string.IsNullOrEmpty(token) && role == "Participant")
        {
            var myEnrolments = await _apiClient.GetMyEnrolmentsAsync(token);
            isEnrolled = myEnrolments.Any(e => e.EventId == id);
        }

        var vm = new EventDetailViewModel
        {
            Event = eventDetail,
            CurrentUserRole = role,
            IsEnrolled = isEnrolled
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int eventId, int categoryId)
    {
        var token = HttpContext.Session.GetString("SessionToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrEmpty(token))
        {
            TempData["Info"] = "Please log in with a Participant account to enter this event.";
            return RedirectToAction("Login", "Account", new { returnUrl = $"/Events/Details/{eventId}" });
        }

        if (role != "Participant")
        {
            TempData["Error"] = "Only athlete Participant accounts can enter race events. Organisers cannot register as participants.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var enrolment = await _apiClient.EnrolAsync(token, new CreateEnrolmentDto
        {
            EventId = eventId,
            CategoryId = categoryId
        });

        if (enrolment == null)
        {
            TempData["Error"] = "Could not complete enrolment. You may already be registered for this race, or registration is closed.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = $"Congratulations! You are officially entered in {enrolment.EventTitle}. Assigned Race Bib: #{enrolment.BibNumber}.";
        return RedirectToAction("MyEvents", "Participant");
    }
}
