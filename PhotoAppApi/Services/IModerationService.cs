using PhotoAppApi.Models;

namespace PhotoAppApi.Services
{
    public interface IModerationService
    {
        Task<ModerationResult> CheckImageAsync(Stream imageStream, string fileName, string contentType);
    }
}
