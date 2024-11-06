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
    public class GitHubApiServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<GitHubApiService>> _mockLogger;
        private readonly GitHubApiService _service;

        public GitHubApiServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<GitHubApiService>>();

            // Set up mock configuration
            _mockConfiguration.Setup(config => config["GitHubApi:ApiKey"]).Returns("your-api-key");
            _mockConfiguration.Setup(config => config["GitHubApi:BaseUrl"]).Returns("https://api.github.com/users/");

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _service = new GitHubApiService(httpClient, _mockConfiguration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetUserRepositoriesAsync_ReturnsRepositories_WhenSuccessful()
        {
            // Arrange
            var username = "testuser";
            var repositories = new List<Repository>
        {
            new Repository { Id = 1, Name = "Repo1", FullName = "testuser/Repo1", HtmlUrl = "https://github.com/testuser/Repo1", IsPrivate = false, Description = "First repository", Language = "C#" },
            new Repository { Id = 2, Name = "Repo2", FullName = "testuser/Repo2", HtmlUrl = "https://github.com/testuser/Repo2", IsPrivate = false, Description = "Second repository", Language = "JavaScript" }
        };

            // Set up the mock response
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(repositories)
                });

            // Act
            var result = await _service.GetUserRepositoriesAsync(username);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Repo1", result[0].Name);
            Assert.Equal("Repo2", result[1].Name);
        }

        [Fact]
        public async Task GetUserRepositoriesAsync_ReturnsEmptyList_WhenHttpRequestExceptionThrown()
        {
            // Arrange
            var username = "testuser";

            // Set up the HttpClient to throw an HttpRequestException when a request is made
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _service.GetUserRepositoriesAsync(username);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Verify that the result is an empty list

            // Verify that Log was called for the HttpRequestException
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((obj, type) => obj.ToString().Contains("HTTP request error")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), // Note: No need for Exception? here
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserRepositoriesAsync_ReturnsEmptyList_WhenUnexpectedExceptionThrown()
        {
            // Arrange
            var username = "testuser";

            // Simulate an unexpected exception being thrown
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new Exception("Simulated general exception"));

            // Act
            var result = await _service.GetUserRepositoriesAsync(username);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // Verify that the result is an empty list

            // Verify that Log was called for the unexpected exception
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