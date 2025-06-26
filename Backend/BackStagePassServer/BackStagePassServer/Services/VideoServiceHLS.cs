
using System.Diagnostics;

namespace BackStagePassServer.Services;

public class VideoServiceHLS : IVideoService
{
	private readonly string _videoDirectory;
	private readonly string _ffmpegPath;

	public VideoServiceHLS(IWebHostEnvironment env)
	{
		_videoDirectory = Path.Combine(env.WebRootPath, "videos", "stream");
		if (!Directory.Exists(_videoDirectory))
			Directory.CreateDirectory(_videoDirectory);

		// Путь к локальному ffmpeg.exe в проекте
		_ffmpegPath = Path.Combine(env.ContentRootPath, "ffmpeg", "ffmpeg.exe");
	}

	public async Task<string> SaveVideoAsync(IFormFile file)
	{
		if (file == null || file.Length == 0)
			throw new ArgumentException("Invalid video file.");

		var extension = Path.GetExtension(file.FileName).ToLower();
		if (!new[] { ".mp4", ".avi", ".mkv", ".mov" }.Contains(extension))
			throw new ArgumentException("Unsupported file type.");

		var videoId = Guid.NewGuid().ToString();
		var videoFolder = Path.Combine(_videoDirectory, videoId);
		Directory.CreateDirectory(videoFolder);

		var inputPath = Path.Combine(videoFolder, "input" + extension);

		using (var stream = new FileStream(inputPath, FileMode.Create))
			await file.CopyToAsync(stream);

		await RunFfmpegAsync($"""
		-i "{inputPath}" -vf scale=640:360 -c:v h264 -b:v 800k -c:a aac -ar 48000 -preset veryfast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/360p_%03d.ts" "{videoFolder}/360p.m3u8"
		""");

		await RunFfmpegAsync($"""
		-i "{inputPath}" -vf scale=1280:720 -c:v h264 -b:v 2800k -c:a aac -ar 48000 -preset veryfast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/720p_%03d.ts" "{videoFolder}/720p.m3u8"
		""");

		await RunFfmpegAsync($"""
		-i "{inputPath}" -c:v h264 -b:v 5000k -c:a aac -ar 48000 -preset veryfast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/original_%03d.ts" "{videoFolder}/original.m3u8"
		""");



		//Use this instead on a server with updated drivers and hardware that support ffmpeg h264_nvenc for better performance!!!

		//// 360p
		//await RunFfmpegAsync($"""
		//-i "{inputPath}" -vf scale=640:360 -c:v h264_nvenc -b:v 800k -c:a aac -ar 48000 -preset fast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/360p_%03d.ts" "{videoFolder}/360p.m3u8"
		//""");

		//// 720p
		//await RunFfmpegAsync($"""
		//-i "{inputPath}" -vf scale=1280:720 -c:v h264_nvenc -b:v 2800k -c:a aac -ar 48000 -preset fast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/720p_%03d.ts" "{videoFolder}/720p.m3u8"
		//""");

		//// original
		//await RunFfmpegAsync($"""
		//-i "{inputPath}" -c:v h264_nvenc -b:v 5000k -c:a aac -ar 48000 -preset fast -g 48 -hls_time 10 -hls_list_size 0 -hls_segment_filename "{videoFolder}/original_%03d.ts" "{videoFolder}/original.m3u8"
		//""");


		File.Delete(inputPath);

		// Создание master.m3u8
		var masterPath = Path.Combine(videoFolder, "master.m3u8");
		var masterContent = """
			#EXTM3U
			#EXT-X-STREAM-INF:BANDWIDTH=1000000,RESOLUTION=640x360
			360p.m3u8
			#EXT-X-STREAM-INF:BANDWIDTH=3000000,RESOLUTION=1280x720
			720p.m3u8
			#EXT-X-STREAM-INF:BANDWIDTH=5000000,RESOLUTION=1920x1080
			original.m3u8
			""";
		await File.WriteAllTextAsync(masterPath, masterContent.Trim());


		// Возвращаем относительный путь к master.m3u8
		return $"/videos/stream/{videoId}/master.m3u8";
	}

	public List<string> GetAllVideoUrls(HttpRequest request)
	{
		var baseUrl = $"{request.Scheme}://{request.Host}";
		var folders = Directory.GetDirectories(_videoDirectory);

		return folders
			.Select(folder => Path.GetFileName(folder))
			.Select(id => $"{baseUrl}/videos/stream/{id}/master.m3u8")
			.ToList();
	}

	public async Task DeleteVideoByUrlAsync(string videoUrl)
	{
		if (string.IsNullOrWhiteSpace(videoUrl))
			throw new ArgumentException("URL is empty.");

		var folderName = Path.GetFileName(Path.GetDirectoryName(new Uri(videoUrl).LocalPath));
		var folderPath = Path.Combine(_videoDirectory, folderName);

		if (Directory.Exists(folderPath))
			await Task.Run(() => Directory.Delete(folderPath, true));
	}

	private async Task RunFfmpegAsync(string arguments)
	{
		var psi = new ProcessStartInfo
		{
			FileName = _ffmpegPath,
			Arguments = arguments,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = Process.Start(psi);
		string stderr = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
			throw new Exception("FFmpeg error: " + stderr);
	}
}
