using HackerAPI.Models;

namespace HackerAPI.IHacker
{
    public interface IHackerservice
    {
        Task<List<StoryDetail>> GetAllStories();
    }
}
