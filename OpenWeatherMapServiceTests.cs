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

    public class OpenWeatherMapServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<OpenWeatherMapService>> _mockLogger;
        private readonly OpenWeatherMapService _service;

        public OpenWeatherMapServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<OpenWeatherMapService>>();

            // Set up mock configuration
            _mockConfiguration.Setup(config => config["OpenWeatherMap:ApiKey"]).Returns("your-api-key");
            _mockConfiguration.Setup(config => config["OpenWeatherMap:BaseUrl"]).Returns("https://api.openweathermap.org/data/2.5/");

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new OpenWeatherMapService(httpClient, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsWeatherResponse_WhenSuccessful()
        {
            var city = "Athens";
            var weatherResponse = new WeatherResponse { Name = "Athens", Main = new Main { Temp = 20.5 } };

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(weatherResponse)
                });

            var result = await _service.GetCurrentWeatherAsync(city);

            Assert.NotNull(result);
            Assert.Equal(city, result.Name);
            Assert.Equal(20.5, result.Main.Temp);
        }

        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsNull_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            var city = "Athens";

            // Set up the HttpClient to throw an HttpRequestException when a request is made
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            var service = new OpenWeatherMapService(new HttpClient(_mockHttpMessageHandler.Object), _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetCurrentWeatherAsync(city);

            // Assert
            Assert.Null(result); // Verify that the result is null

            // Verify that LogError was called for the HttpRequestException
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((obj, type) => obj.ToString().Contains("Error retrieving weather data for city: Athens")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), // Changed to Exception?
                Times.Once
            );



        }


        [Fact]
        public async Task GetCurrentWeatherAsync_ReturnsNull_WhenGeneralExceptionThrown()
        {
            // Arrange
            var city = "Athens";
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Simulate an unexpected exception being thrown
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Simulated general exception"));

            var service = new OpenWeatherMapService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetCurrentWeatherAsync(city);

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