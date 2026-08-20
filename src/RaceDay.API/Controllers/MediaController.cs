using Microsoft.AspNetCore.Mvc;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public MediaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Serves uploaded images (banners and profile pictures) from local cloud-replica storage.
    /// </summary>
    [HttpGet("{container}/{fileName}")]
    public IActionResult GetFile(string container, string fileName)
    {
        var cleanContainer = Path.GetFileName(container);
        var cleanFileName = Path.GetFileName(fileName);

        var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var filePath = Path.Combine(webRoot, "uploads", cleanContainer, cleanFileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Not Found", message = "The requested media file does not exist." });
        }

        var extension = Path.GetExtension(cleanFileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType);
    }
}
