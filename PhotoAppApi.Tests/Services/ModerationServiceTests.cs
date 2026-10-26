using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class ModerationServiceTests
    {
        [Fact]
        public async Task CheckImageAsync_WhenCancelled_ThrowsOperationCanceledException()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new OperationCanceledException());

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8000")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("ModerationClient")).Returns(httpClient);

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["ModerationApiKey"]).Returns("test-key");

            var service = new ModerationService(factoryMock.Object, configMock.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var stream = new MemoryStream(new byte[10]);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.CheckImageAsync(stream, "test.jpg", "image/jpeg", cts.Token)
            );
        }

        [Fact]
        public async Task CheckImageAsync_HttpError_FailsClosedWithIsNsfwTrue()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8000")
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient("ModerationClient")).Returns(httpClient);

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["ModerationApiKey"]).Returns("test-key");

            var service = new ModerationService(factoryMock.Object, configMock.Object);

            using var stream = new MemoryStream(new byte[10]);

            var result = await service.CheckImageAsync(stream, "test.jpg", "image/jpeg", CancellationToken.None);

            Assert.True(result.IsNsfw);
        }
    }
}
