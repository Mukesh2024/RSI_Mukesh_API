using HackerAPI.IHacker;
using HackerAPI.Models;
using HackerAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using HackerAPI.Hacker;
using HackerAPI;
using System.Text.Json;
using Moq.Protected;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

public class HackerControllerTests
{
    /// <summary>
    /// Unit test case for HackerController
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetAllStories_ReturnsOkResultWithData()
    {
        // Arrange
        var mockHackerService = new Mock<IHackerservice>();
        mockHackerService.Setup(service => service.GetAllStories())
            .ReturnsAsync(new List<StoryDetail>
            {
                new StoryDetail
                {
                    Title = "Test Title",
                    NewsArticle = "https://www.google.com"
                },
            });

        var controller = new HackerController(mockHackerService.Object);

        // Act
        var result = await controller.GetAllStories();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var jsonDataFinal = Assert.IsType<string>(okResult.Value);
        var deserializedResult = JsonSerializer.Deserialize<List<StoryDetail>>(jsonDataFinal);

        Assert.NotNull(deserializedResult);
        Assert.NotEmpty(deserializedResult);
    }

    /// <summary>
    /// Unit Test case for HackerController
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetAllStories_ReturnsBadRequestResultOnError()
    {
        // Arrange
        var mockHackerService = new Mock<IHackerservice>();
        mockHackerService.Setup(service => service.GetAllStories())
            .ThrowsAsync(new HttpRequestException("Test error message"));

        var controller = new HackerController(mockHackerService.Object);

        // Act
        var result = await controller.GetAllStories();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Error while fetching the stories: Test error message", badRequestResult.Value);
    }

    /// <summary>
    /// Unit Test case for Hacker service class
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetAllStories_ReturnsListOfStoryDetails()
    {
        // Arrange
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        var mockCacheEntry = new Mock<ICacheEntry>();
        string? keyPayload = null;
        var memoryCacheMock = new Mock<IMemoryCache>();
        memoryCacheMock
        .Setup(mc => mc.CreateEntry(It.IsAny<object>()))
        .Callback((object k) => keyPayload = (string)k)
        .Returns(mockCacheEntry.Object);


        // Define the test configuration with multiple URLs
        var testConfiguration = new Dictionary<string, string>
        {
            { "HackerApiSettings:BaseApiUrl", "https://hacker-news.firebaseio.com/v0" }
        };

        // Create options with the test configuration
        var optionsMock = Options.Create(new ConfigurationBuilder()
            .AddInMemoryCollection(testConfiguration)
            .Build()
            .GetSection("HackerApiSettings")
            .Get<HackerApiSettings>());

        var hackerService = new HackerService(httpClient, memoryCacheMock.Object, optionsMock);

        // // Mocking story IDs
        var topStoriesResponse = new List<int> { 1, 2, 3 };
        var topStoriesJsonResponse = JsonSerializer.Serialize(topStoriesResponse);

        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                // Check if the request URL contains "topstories" for first API
                if (request.RequestUri.ToString().Contains("topstories"))
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(topStoriesJsonResponse),
                    };
                }

                // Check if the request URL contains "Item" for second API
                if (request.RequestUri.ToString().Contains("item"))
                {
                    // Mocking the response from GetStorydetail for each story ID
                    var storyDetailResponse = new StoryDetails
                    {
                        by = "author",
                        descendants = 5,
                        id = 123,
                        kids = new JsonArray { 456, 789 },
                        score = 42,
                        time = 1636411111,
                        title = "Sample Title",
                        type = "story",
                        url = "https://example.com"
                    };

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(storyDetailResponse)),
                    };
                }
                // If no match, return a default response
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ReasonPhrase = "Endpoint not found.",
                };
            });
        // Act
        var result = await hackerService.GetAllStories();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }
}
