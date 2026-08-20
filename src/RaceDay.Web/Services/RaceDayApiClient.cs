using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.API.DTOs;

namespace RaceDay.Web.Services;

public class RaceDayApiClient : IRaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<RaceDayApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RaceDayApiClient(HttpClient http, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    private void SetSessionHeader(string? sessionToken)
    {
        _http.DefaultRequestHeaders.Remove("X-Session-Token");
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            _http.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/auth/login", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<AuthResponseDto>(stream, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API Login");
            return null;
        }
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/auth/register", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<AuthResponseDto>(stream, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling API Register");
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.PostAsync("/api/auth/logout", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> GetMeAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync("/api/auth/me");
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<UserDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserProfileDto?> GetProfileAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync("/api/profile");
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<UserProfileDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserDto?> UpdateProfileAsync(string sessionToken, UpdateProfileDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync("/api/profile", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<UserDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> UploadProfilePictureAsync(string sessionToken, Stream stream, string fileName, string contentType)
    {
        try
        {
            SetSessionHeader(sessionToken);
            using var formData = new MultipartFormDataContent();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formData.Add(streamContent, "file", fileName);

            var response = await _http.PostAsync("/api/profile/picture", formData);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("profilePictureUrl").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile picture");
            return null;
        }
    }

    public async Task<List<EventDto>> GetEventsAsync(string? type = null, string? search = null)
    {
        try
        {
            var url = "/api/events";
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(type)) query.Add($"type={Uri.EscapeDataString(type)}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (query.Count > 0) url += "?" + string.Join("&", query);

            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new();

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<EventDto>>(stream, _jsonOptions) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events");
            return new();
        }
    }

    public async Task<EventDetailDto?> GetEventByIdAsync(int id)
    {
        try
        {
            var response = await _http.GetAsync($"/api/events/{id}");
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<EventDetailDto>(stream, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event details for id {Id}", id);
            return null;
        }
    }

    public async Task<EventDto?> CreateEventAsync(string sessionToken, CreateEventDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/events", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<EventDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<EventDto?> UpdateEventAsync(string sessionToken, int id, UpdateEventDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"/api/events/{id}", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<EventDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteEventAsync(string sessionToken, int id)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.DeleteAsync($"/api/events/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> UploadEventBannerAsync(string sessionToken, int eventId, Stream stream, string fileName, string contentType)
    {
        try
        {
            SetSessionHeader(sessionToken);
            using var formData = new MultipartFormDataContent();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formData.Add(streamContent, "file", fileName);

            var response = await _http.PostAsync($"/api/events/{eventId}/banner", formData);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("bannerImageUrl").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading banner for event {EventId}", eventId);
            return null;
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesByEventAsync(int eventId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/categories/event/{eventId}");
            if (!response.IsSuccessStatusCode) return new();
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<CategoryDto>>(stream, _jsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<CategoryDto?> CreateCategoryAsync(string sessionToken, CreateCategoryDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/categories", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<CategoryDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteCategoryAsync(string sessionToken, int id)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.DeleteAsync($"/api/categories/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EnrolmentDto?> EnrolAsync(string sessionToken, CreateEnrolmentDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/enrolments", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<EnrolmentDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<EnrolmentDto>> GetMyEnrolmentsAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync("/api/enrolments/my");
            if (!response.IsSuccessStatusCode) return new();
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<EnrolmentDto>>(stream, _jsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(string sessionToken, int eventId)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync($"/api/enrolments/event/{eventId}");
            if (!response.IsSuccessStatusCode) return new();
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<EnrolmentDto>>(stream, _jsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<bool> UpdateEnrolmentStatusAsync(string sessionToken, int enrolmentId, string status)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(new { status }), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync($"/api/enrolments/{enrolmentId}/status", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ResultDto>?> CaptureResultsAsync(string sessionToken, CaptureResultsBatchDto dto)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/results/capture", content);
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<ResultDto>>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ResultDto>> GetEventResultsAsync(int eventId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/results/event/{eventId}");
            if (!response.IsSuccessStatusCode) return new();
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<ResultDto>>(stream, _jsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<PersonalResultDto>> GetMyResultsAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync("/api/results/my");
            if (!response.IsSuccessStatusCode) return new();
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<PersonalResultDto>>(stream, _jsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<OrganiserDashboardDto?> GetOrganiserDashboardAsync(string sessionToken)
    {
        try
        {
            SetSessionHeader(sessionToken);
            var response = await _http.GetAsync("/api/organiser/dashboard");
            if (!response.IsSuccessStatusCode) return null;
            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<OrganiserDashboardDto>(stream, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
