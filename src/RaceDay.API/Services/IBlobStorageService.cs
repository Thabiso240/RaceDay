namespace RaceDay.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string containerName, string contentType);
    Task<bool> DeleteFileAsync(string fileUrl, string containerName);
}
