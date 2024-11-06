using APIAggregator.Models;
using APIAggregator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;

namespace APIAggregator.Tests
{
    public class CatFactsServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<CatFactsService>> _mockLogger;
        private readonly CatFactsService _service;

        public CatFactsServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<CatFactsService>>();

            // Set up mock configuration
            _mockConfiguration.Setup(config => config["CatFactsApi:BaseUrl"]).Returns("https://catfact.ninja/facts");

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new CatFactsService(httpClient, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetRandomCatFactsAsync_ReturnsCatFacts_WhenSuccessful()
        {
            // Arrange
            var catFactsResponse = new CatFactsResponse
            {
                CurrentPage = 1,
                Data = new List<CatFact>
                {
                    new CatFact { Fact = "Cats have five toes on their front paws, but only four toes on their back paws.", Length = 70 },
                    new CatFact { Fact = "A group of cats is called a clowder.", Length = 29 }
                },
                LastPage = 34,
                LastPageUrl = "https://catfact.ninja/facts?page=34",
                Path = "https://catfact.ninja/facts",
                PerPage = 10,
                Total = 332
            };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(catFactsResponse)
                });

            // Act
            var result = await _service.GetRandomCatFactsAsync(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(catFactsResponse.CurrentPage, result.CurrentPage);
            Assert.Equal(catFactsResponse.Data.Count, result.Data.Count);
            Assert.Equal(catFactsResponse.Data[0].Fact, result.Data[0].Fact);
        }

        [Fact]
        public async Task GetRandomCatFactsAsync_ReturnsNull_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.GetRandomCatFactsAsync(2);

            // Assert
            Assert.Null(result); // Verify that the result is null

            // Verify that LogError was called for the HttpRequestException
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((obj, type) => obj.ToString().Contains("HTTP request to")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetRandomCatFactsAsync_ReturnsNull_WhenGeneralExceptionThrown()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Simulated general exception"));

            // Act
            var result = await _service.GetRandomCatFactsAsync(2);

            // Assert
            Assert.Null(result); // Verify that the result is null

            // Verify that LogError was called with any Exception and a message containing "An unexpected error occurred"
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((obj, type) => obj.ToString().Contains("An unexpected error occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }
    }
}
