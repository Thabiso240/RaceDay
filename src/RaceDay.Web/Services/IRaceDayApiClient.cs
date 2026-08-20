using RaceDay.API.DTOs;

namespace RaceDay.Web.Services;

public interface IRaceDayApiClient
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);
    Task<bool> LogoutAsync(string sessionToken);
    Task<UserDto?> GetMeAsync(string sessionToken);

    Task<UserProfileDto?> GetProfileAsync(string sessionToken);
    Task<UserDto?> UpdateProfileAsync(string sessionToken, UpdateProfileDto dto);
    Task<string?> UploadProfilePictureAsync(string sessionToken, Stream stream, string fileName, string contentType);

    Task<List<EventDto>> GetEventsAsync(string? type = null, string? search = null);
    Task<EventDetailDto?> GetEventByIdAsync(int id);
    Task<EventDto?> CreateEventAsync(string sessionToken, CreateEventDto dto);
    Task<EventDto?> UpdateEventAsync(string sessionToken, int id, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(string sessionToken, int id);
    Task<string?> UploadEventBannerAsync(string sessionToken, int eventId, Stream stream, string fileName, string contentType);

    Task<List<CategoryDto>> GetCategoriesByEventAsync(int eventId);
    Task<CategoryDto?> CreateCategoryAsync(string sessionToken, CreateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(string sessionToken, int id);

    Task<EnrolmentDto?> EnrolAsync(string sessionToken, CreateEnrolmentDto dto);
    Task<List<EnrolmentDto>> GetMyEnrolmentsAsync(string sessionToken);
    Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(string sessionToken, int eventId);
    Task<bool> UpdateEnrolmentStatusAsync(string sessionToken, int enrolmentId, string status);

    Task<List<ResultDto>?> CaptureResultsAsync(string sessionToken, CaptureResultsBatchDto dto);
    Task<List<ResultDto>> GetEventResultsAsync(int eventId);
    Task<List<PersonalResultDto>> GetMyResultsAsync(string sessionToken);

    Task<OrganiserDashboardDto?> GetOrganiserDashboardAsync(string sessionToken);
}
