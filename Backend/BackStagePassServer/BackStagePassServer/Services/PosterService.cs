using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BackStagePassServer.Services;



public class PosterService : IPosterService
{
	private readonly string _posterOriginalDirectory;
	private readonly string _poster480pDirectory;
	private readonly string _profilesDirectory;

	public PosterService(IWebHostEnvironment env)
	{
		_posterOriginalDirectory = Path.Combine(env.WebRootPath, "posters_original");
		if (!Directory.Exists(_posterOriginalDirectory))
			Directory.CreateDirectory(_posterOriginalDirectory);

		_poster480pDirectory = Path.Combine(env.WebRootPath, "posters_480p");
		if (!Directory.Exists(_poster480pDirectory))
			Directory.CreateDirectory(_poster480pDirectory);

		_profilesDirectory = Path.Combine(env.WebRootPath, "profiles");
		if (!Directory.Exists(_profilesDirectory))
			Directory.CreateDirectory(_profilesDirectory);
	}

	public async Task<string> SavePosterAsync(IFormFile file)
	{
		if (file == null || file.Length == 0)
			throw new ArgumentException("Invalid poster file.");

		var extension = Path.GetExtension(file.FileName);
		if (string.IsNullOrEmpty(extension) || !new[] { ".jpg", ".jpeg", ".png"}.Contains(extension.ToLower()))
			throw new ArgumentException("Unsupported file type. Only .jpg, .jpeg, .png are allowed.");

		var fileName = Guid.NewGuid().ToString() + extension;
		var fullPath = Path.Combine(_posterOriginalDirectory, fileName);

		using (var stream = new FileStream(fullPath, FileMode.Create))
		{
			await file.CopyToAsync(stream);
		}

		// Создаем и сохраняем 480p версию
		using (var image = await Image.LoadAsync(fullPath))
		{
			var newSize = Get480pSize(image.Width, image.Height);
			image.Mutate(x => x.Resize(newSize.width, newSize.height));

			var compressedPath = Path.Combine(_poster480pDirectory, fileName);
			await image.SaveAsync(compressedPath);
		}

		return fileName;
	}

	private (int width, int height) Get480pSize(int originalWidth, int originalHeight)
	{
		const int targetHeight = 480;
		if (originalHeight <= targetHeight)
			return (originalWidth, originalHeight);

		double ratio = (double)targetHeight / originalHeight;
		int newWidth = (int)(originalWidth * ratio);
		return (newWidth, targetHeight);
	}


	public List<string> GetAllPosterUrls(HttpRequest request)
	{
		var baseUrl = $"{request.Scheme}://{request.Host}";

		var files = Directory.GetFiles(_poster480pDirectory);
		var urls = files
			.Select(Path.GetFileName)
			.Select(fileName => $"{baseUrl}/posters_480p/{fileName}")
			.ToList();

		return urls;
	}

	public async Task<string> SaveProfileAsync(IFormFile picture)
	{
		if (picture == null || picture.Length == 0)
			throw new ArgumentException("Invalid profile picture.");

		var extension = Path.GetExtension(picture.FileName);
		if (string.IsNullOrEmpty(extension) || !new[] { ".jpg", ".jpeg", ".png" }.Contains(extension.ToLower()))
			throw new ArgumentException("Unsupported file type. Only .jpg, .jpeg, .png are allowed.");


		var fileName = Guid.NewGuid().ToString() + extension;
		var fullPath = Path.Combine(_profilesDirectory, fileName);

		using (var image = await Image.LoadAsync(picture.OpenReadStream()))
		{
			// Target even smaller than 480p, e.g., 200x200 (square thumbnail)
			const int targetSize = 200;
			int newWidth, newHeight;
			if (image.Width > image.Height)
			{
				newHeight = targetSize;
				newWidth = (int)((double)image.Width / image.Height * targetSize);
			}
			else
			{
				newWidth = targetSize;
				newHeight = (int)((double)image.Height / image.Width * targetSize);
			}
			image.Mutate(x => x.Resize(newWidth, newHeight).Crop(new Rectangle((newWidth - targetSize) / 2, (newHeight - targetSize) / 2, targetSize, targetSize)));

			await image.SaveAsync(fullPath);
		}

		return fileName;
	}
	public async Task<bool> DeleteFileByNameAsync(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			return false;

		bool deleted = false;
		var directories = new[] { _posterOriginalDirectory, _poster480pDirectory, _profilesDirectory };

		foreach (var dir in directories)
		{
			var filePath = Path.Combine(dir, fileName);
			if (File.Exists(filePath))
			{
				try
				{
					await Task.Run(() => File.Delete(filePath));
					deleted = true;
				}
				catch
				{
					// Optionally log error
				}
			}
		}

		return deleted;
	}


}
