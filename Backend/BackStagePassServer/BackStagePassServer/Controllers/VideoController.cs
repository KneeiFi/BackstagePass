using BackStagePassServer.DTOs;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
	private readonly IVideoService _videoService;
	private readonly AuthService _authService;
	private readonly AppDbContext _db;
	public VideoController(AuthService authService, AppDbContext db, IVideoService videoService)
	{
		_videoService = videoService;
		_authService = authService;
		_db = db;
	}

	[HttpPost("upload")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> Upload([FromForm] UploadVideoRequest request,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized("Недействительный токен доступа");

		if (user.IsEmailConfirmed == 0)
			return BadRequest("Email не подтвержден. Пожалуйста, подтвердите ваш email перед загрузкой видео.");


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
