namespace BackStagePassServer.Services
{
	public interface IVideoService
	{
		Task<string> SaveVideoAsync(IFormFile file);
		List<string> GetAllVideoUrls(HttpRequest request);
		Task DeleteVideoByUrlAsync(string videoUrl);
	}
}

