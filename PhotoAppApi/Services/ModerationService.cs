using log4net;
using PhotoAppApi.Models;

namespace PhotoAppApi.Services
{
    public class ModerationService : IModerationService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ModerationService));

        private readonly HttpClient _httpClient;

        public ModerationService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ModerationClient");
        }

        public async Task<ModerationResult> CheckImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellation = default)
        {
            try
            {
                using var content = new MultipartFormDataContent();

                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                content.Add(streamContent, "file", fileName);

                var response = await _httpClient.PostAsync("/moderate", content, cancellation);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ModerationResult>(cancellation);
                return result ?? new ModerationResult { IsNsfw = true };
            }
            catch (Exception ex)
            {
                log.Error("Error while checking image moderation", ex);
                return new ModerationResult { IsNsfw = true }; // Default to NSFW if there's an error
            }
        }
    }
}
