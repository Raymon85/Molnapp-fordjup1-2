namespace RecruitmentPortal.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<bool> IsAvailableAsync();
}
