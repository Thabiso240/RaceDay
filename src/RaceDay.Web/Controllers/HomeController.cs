using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRaceDayApiClient _apiClient;

    public HomeController(IRaceDayApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _apiClient.GetEventsAsync();
        var vm = new EventBrowseViewModel
        {
            Events = events.Take(6).ToList()
        };
        return View(vm);
    }

    public IActionResult Error()
    {
        return View();
    }
}
