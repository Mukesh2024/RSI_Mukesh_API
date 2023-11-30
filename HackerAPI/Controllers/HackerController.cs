using HackerAPI.IHacker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HackerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HackerController : ControllerBase
    {
        private readonly IHackerservice _hackerService;

        public HackerController(IHackerservice hackerService)
        {
            _hackerService = hackerService;
        }
        /// <summary>
        /// Get Top 200 user stories details in Json Format
        /// </summary>
        /// <returns>User Story details</returns>
        [HttpGet("GetAllStories")]
        public async Task<ActionResult> GetAllStories()
        {
            try
            {
                var lstStories = await _hackerService.GetAllStories();
                var jsonDataFinal = JsonSerializer.Serialize(lstStories);
                return Ok(jsonDataFinal);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest($"Error while fetching the stories: {ex.Message}");
            }
        }
    }
}