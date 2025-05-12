using BackStagePassServer.DTOs;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
	private readonly IVideoService _videoService;

	public VideoController(IVideoService videoService)
	{
		_videoService = videoService;
	}

	[HttpPost("upload")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> Upload([FromForm] UploadVideoRequest request)
	{
		if (request.File == null)
			return BadRequest("Файл не выбран");

		var relativePath = await _videoService.SaveVideoAsync(request.File);
		var fullUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";

		return Ok(new
		{
			url = fullUrl
		});
	}

	[HttpGet("all")]
	public IActionResult GetAll()
	{
		var urls = _videoService.GetAllVideoUrls(Request);
		return Ok(urls);
	}
}
