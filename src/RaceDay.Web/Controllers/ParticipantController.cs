using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ParticipantController : Controller
{
    private readonly IRaceDayApiClient _apiClient;

    public ParticipantController(IRaceDayApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    private string? EnsureParticipant()
    {
        var token = HttpContext.Session.GetString("SessionToken");
        var role = HttpContext.Session.GetString("UserRole");

        if (string.IsNullOrEmpty(token) || role != "Participant")
            return null;

        return token;
    }

    public async Task<IActionResult> MyEvents()
    {
        var token = EnsureParticipant();
        if (token == null)
        {
            TempData["Error"] = "Access restricted. Please log in with an athlete Participant account.";
            return RedirectToAction("Login", "Account", new { returnUrl = "/Participant/MyEvents" });
        }

        var enrolments = await _apiClient.GetMyEnrolmentsAsync(token);
        return View(enrolments);
    }

    public async Task<IActionResult> MyResults()
    {
        var token = EnsureParticipant();
        if (token == null)
        {
            TempData["Error"] = "Access restricted. Please log in with a Participant account.";
            return RedirectToAction("Login", "Account", new { returnUrl = "/Participant/MyResults" });
        }

        var results = await _apiClient.GetMyResultsAsync(token);
        return View(results);
    }
}
