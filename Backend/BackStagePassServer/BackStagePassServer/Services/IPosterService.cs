namespace BackStagePassServer.Services;

public interface IPosterService
{
	Task<string> SavePosterAsync(IFormFile file);
	List<string> GetAllPosterUrls(HttpRequest request);
}
