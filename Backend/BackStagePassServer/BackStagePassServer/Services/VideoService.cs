
namespace BackStagePassServer.Services;

public class VideoService : IVideoService
{
	private readonly string _videoDirectory;

	public VideoService(IWebHostEnvironment env)
	{
		_videoDirectory = Path.Combine(env.WebRootPath, "videos");
		if (!Directory.Exists(_videoDirectory))
			Directory.CreateDirectory(_videoDirectory);
	}

	public async Task<string> SaveVideoAsync(IFormFile file)
	{
		if (file == null || file.Length == 0)
			throw new ArgumentException("Invalid video file.");

		var extension = Path.GetExtension(file.FileName);
		// Проверяем расширение файла
		if (string.IsNullOrEmpty(extension) || !new[] { ".mp4", ".avi", ".mkv", ".mov" }.Contains(extension.ToLower()))
			throw new ArgumentException("Unsupported file type. Only .mp4, .avi, .mkv, and .mov are allowed.");

		var fileName = Guid.NewGuid().ToString() + extension;
		var fullPath = Path.Combine(_videoDirectory, fileName);

		using (var stream = new FileStream(fullPath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		// Возвращаем относительный путь — клиенту можно будет собрать полный URL сам
		return $"/videos/{fileName}";
	}
	public List<string> GetAllVideoUrls(HttpRequest request)
	{
		var baseUrl = $"{request.Scheme}://{request.Host}";

		var files = Directory.GetFiles(_videoDirectory);
		var urls = files
			.Select(Path.GetFileName)
			.Select(fileName => $"{baseUrl}/videos/{fileName}")
			.ToList();

		return urls;
	}

	public async Task DeleteVideoByUrlAsync(string videoUrl)
	{
		if (string.IsNullOrWhiteSpace(videoUrl))
			throw new ArgumentException("Video URL cannot be null or empty.");

		var fileName = Path.GetFileName(new Uri(videoUrl, UriKind.RelativeOrAbsolute).LocalPath);
		if (string.IsNullOrEmpty(fileName))
			throw new ArgumentException("Invalid video URL.");

		var filePath = Path.Combine(_videoDirectory, fileName);
		if (!File.Exists(filePath))
			throw new FileNotFoundException("Video file not found.", fileName);

		await Task.Run(() => File.Delete(filePath));
	}


}
