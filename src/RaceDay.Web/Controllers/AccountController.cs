using Microsoft.AspNetCore.Mvc;
using RaceDay.API.DTOs;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly IRaceDayApiClient _apiClient;

    public AccountController(IRaceDayApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _apiClient.LoginAsync(new LoginRequestDto
        {
            Email = model.Email,
            Password = model.Password
        });

        if (response == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            return View(model);
        }

        // Store user state in server-side session
        HttpContext.Session.SetString("SessionToken", response.SessionToken);
        HttpContext.Session.SetInt32("UserId", response.UserId);
        HttpContext.Session.SetString("UserName", response.FullName);
        HttpContext.Session.SetString("UserEmail", response.Email);
        HttpContext.Session.SetString("UserRole", response.Role);
        if (!string.IsNullOrEmpty(response.ProfilePictureUrl))
        {
            HttpContext.Session.SetString("UserProfilePic", response.ProfilePictureUrl);
        }

        TempData["Success"] = $"Welcome back, {response.FullName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (response.Role == "Organiser")
            return RedirectToAction("Dashboard", "Organiser");

        return RedirectToAction("Index", "Events");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _apiClient.RegisterAsync(new RegisterRequestDto
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = model.Password,
            Role = model.Role,
            PhoneNumber = model.PhoneNumber
        });

        if (response == null)
        {
            ModelState.AddModelError(string.Empty, "Registration failed. The email address may already be registered on RaceDay.");
            return View(model);
        }

        // Establish session
        HttpContext.Session.SetString("SessionToken", response.SessionToken);
        HttpContext.Session.SetInt32("UserId", response.UserId);
        HttpContext.Session.SetString("UserName", response.FullName);
        HttpContext.Session.SetString("UserEmail", response.Email);
        HttpContext.Session.SetString("UserRole", response.Role);

        TempData["Success"] = $"Account created successfully! Welcome to RaceDay as an {response.Role}.";

        if (response.Role == "Organiser")
            return RedirectToAction("Dashboard", "Organiser");

        return RedirectToAction("Index", "Events");
    }

    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Session.GetString("SessionToken");
        if (!string.IsNullOrEmpty(token))
        {
            await _apiClient.LogoutAsync(token);
        }

        HttpContext.Session.Clear();
        TempData["Info"] = "You have been logged out safely.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var token = HttpContext.Session.GetString("SessionToken");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });

        var profile = await _apiClient.GetProfileAsync(token);
        if (profile == null)
            return RedirectToAction("Login");

        var vm = new ProfileViewModel
        {
            UserId = profile.UserId,
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role,
            PhoneNumber = profile.PhoneNumber,
            ProfilePictureUrl = profile.ProfilePictureUrl,
            TotalActivities = profile.TotalEventsOrEnrolments
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var token = HttpContext.Session.GetString("SessionToken");
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        if (!ModelState.IsValid)
            return View(model);

        // Update details
        var updated = await _apiClient.UpdateProfileAsync(token, new UpdateProfileDto
        {
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber
        });

        if (updated != null)
        {
            HttpContext.Session.SetString("UserName", updated.FullName);
        }

        // Handle profile picture upload through API (Azure Blob Storage)
        if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
        {
            using var stream = model.ProfilePicture.OpenReadStream();
            var newUrl = await _apiClient.UploadProfilePictureAsync(
                token, stream, model.ProfilePicture.FileName, model.ProfilePicture.ContentType);

            if (!string.IsNullOrEmpty(newUrl))
            {
                HttpContext.Session.SetString("UserProfilePic", newUrl);
                model.ProfilePictureUrl = newUrl;
            }
        }

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction("Profile");
    }
}
