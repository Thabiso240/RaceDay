using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace RaceDay.API.Services;

/// <summary>
/// Azure Blob Storage Service.
/// Handles cloud blob uploads for event banners and participant profile pictures.
/// Supports live Azure Storage accounts as well as localized environment fallback for offline development.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<AzureBlobStorageService> logger)
    {
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream, 
        string originalFileName, 
        string containerName, 
        string contentType)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        var accountName = _configuration["AzureBlobStorage:AccountName"];
        var accountKey = _configuration["AzureBlobStorage:AccountKey"];

        // If live Azure Blob credentials are provided, perform cloud upload
        if (!string.IsNullOrWhiteSpace(accountName) && !string.IsNullOrWhiteSpace(accountKey) &&
            !accountName.Contains("your-storage-account"))
        {
            try
            {
                var cloudUrl = await UploadToAzureBlobDirectAsync(accountName, accountKey, containerName, safeFileName, fileStream, contentType);
                _logger.LogInformation("Uploaded file successfully to Azure Blob: {Url}", cloudUrl);
                return cloudUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Direct Azure Blob upload failed. Falling back to persistent local storage.");
            }
        }

        // Resilient fallback: store locally inside wwwroot/uploads/{containerName}
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads", containerName);
        Directory.CreateDirectory(uploadsRoot);

        var destinationPath = Path.Combine(uploadsRoot, safeFileName);
        fileStream.Position = 0;
        using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(destStream);
        }

        var baseUrl = _configuration["App:ApiBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5001";
        var fileUrl = $"{baseUrl}/api/media/{containerName}/{safeFileName}";
        _logger.LogInformation("Stored file locally at {Path}, public URL: {Url}", destinationPath, fileUrl);
        return fileUrl;
    }

    public Task<bool> DeleteFileAsync(string fileUrl, string containerName)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.FromResult(false);

        try
        {
            var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads", containerName);
            var filePath = Path.Combine(uploadsRoot, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Url}", fileUrl);
        }

        return Task.FromResult(false);
    }

    private async Task<string> UploadToAzureBlobDirectAsync(
        string accountName, 
        string accountKey, 
        string container, 
        string blobName, 
        Stream stream, 
        string contentType)
    {
        var requestUri = new Uri($"https://{accountName}.blob.core.windows.net/{container}/{blobName}");
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri);

        stream.Position = 0;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var contentBytes = memoryStream.ToArray();

        request.Content = new ByteArrayContent(contentBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content.Headers.Add("x-ms-blob-type", "BlockBlob");

        var date = DateTime.UtcNow.ToString("R");
        request.Headers.Add("x-ms-date", date);
        request.Headers.Add("x-ms-version", "2020-10-02");

        // Construct Shared Key Authorization signature
        var stringToSign = $"PUT\n\n\n{contentBytes.Length}\n\n{contentType}\n\n\n\n\n\n\nx-ms-blob-type:BlockBlob\nx-ms-date:{date}\nx-ms-version:2020-10-02\n/{accountName}/{container}/{blobName}";
        using var hmac = new HMACSHA256(Convert.FromBase64String(accountKey));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        request.Headers.Authorization = new AuthenticationHeaderValue("SharedKey", $"{accountName}:{signature}");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return requestUri.ToString();
    }
}
