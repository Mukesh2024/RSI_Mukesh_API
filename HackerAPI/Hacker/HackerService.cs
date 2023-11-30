using HackerAPI.IHacker;
using HackerAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HackerAPI.Hacker
{
    public class HackerService : IHackerservice
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly HackerApiSettings _apiSettings;

        public HackerService(HttpClient httpClient, IMemoryCache memoryCache, IOptions<HackerApiSettings> apiSettings)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _apiSettings = apiSettings.Value;
        }
        /// <summary>
        /// To get top 200 stories from the API with 2 columns Title and NewsArticle
        /// </summary>
        /// <returns>Json Data</returns>
        /// <exception cref="HttpRequestException"></exception>
        public async Task<List<StoryDetail>> GetAllStories()
        {
            List<StoryDetail> lstStories = new List<StoryDetail>();

            // Get From Memory Cache
            if (_memoryCache.TryGetValue("StoriesData", out List<StoryDetail> cacheStories))
            {
                lstStories = cacheStories;
            }
            else
            {
                var apiUrl = $"{_apiSettings.BaseApiUrl}/topstories.json?print=pretty";

                // Fetch All story ID from API
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    var storiesIds = DesearilizeStoryIDs(jsonData);

                    //Returning Top 200 Stories whose URL is not blank
                    foreach (var storyId in storiesIds)
                    {
                        var StoryDetail = await GetStoryDetail(storyId);
                        if (StoryDetail != null && !string.IsNullOrEmpty(StoryDetail.NewsArticle))
                            lstStories.Add(StoryDetail);

                        if (lstStories.Count >= 200)
                            break;
                    }

                    _memoryCache.Set("StoriesData", lstStories, TimeSpan.FromMinutes(30));
                }
                else
                {
                    // Handle error
                    throw new HttpRequestException("Error while fetching the stories");
                }
            }

            return lstStories;
        }

        /// <summary>
        /// Deserialize the string Jason into List
        /// </summary>
        /// <param name="jsonData"></param>
        /// <returns>List of StoryId</returns>
        private List<int> DesearilizeStoryIDs(string jsonData)
        {
            return JsonSerializer.Deserialize<List<int>>(jsonData)
                .OrderByDescending(id => id)
                .ToList();
        }

        /// <summary>
        /// Returns Detail of Story on the basis of Story ID
        /// </summary>
        /// <param name="storyId"></param>
        /// <returns>It return title and News Article by using API</returns>
        private async Task<StoryDetail> GetStoryDetail(int storyId)
        {

            var apiUrl = $"{_apiSettings.BaseApiUrl}/item/{storyId}.json?print=pretty";
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonDatanew = await response.Content.ReadAsStringAsync();
                var hackerObject = JsonSerializer.Deserialize<StoryDetails>(jsonDatanew);

                if (hackerObject != null && !string.IsNullOrEmpty(hackerObject.url))
                {
                    return new StoryDetail
                    {
                        Title = hackerObject.title,
                        NewsArticle = hackerObject.url
                    };
                }
            }

            return null;
        }
    }

}