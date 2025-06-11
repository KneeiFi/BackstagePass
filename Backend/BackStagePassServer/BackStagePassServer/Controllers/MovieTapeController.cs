using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;



[ApiController]
[Route("api/[controller]")]
public class MovieTapeController : ControllerBase
{
	private readonly IPosterService _posterService;
	private readonly IVideoService _videoService;
	private readonly AuthService _authService;
	private readonly AppDbContext _db;
	public MovieTapeController(AuthService authService, AppDbContext db, IVideoService videoService, IPosterService posterService)
	{
		_videoService = videoService;
		_authService = authService;
		_db = db;
		_posterService = posterService;
	}

	[HttpPost("upload")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> Upload([FromForm] UploadTapeDto request,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before uploading a video." });
		if (user.Role != UserRole.Admin)
			return Forbid("Only admins can upload movie tapes.");
		if (request.Video == null)
			return BadRequest(new { error = "No file selected" });

		try
		{ 
			var relativePath = await _videoService.SaveVideoAsync(request.Video);

			var fullVideoUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";

			var ThumbnailName = await _posterService.SavePosterAsync(request.Thumbnail);

			var movieTape = new MovieTape
			{
				TapeTitle = request.TapeTitle,
				MediaType = request.MediaType,
				VideoUrl = fullVideoUrl,
				ThumbnailUrl = ThumbnailName,
				MovieId = request.MovieId
			};

			_db.MovieTapes.Add(movieTape);
			await _db.SaveChangesAsync();

			return Ok(new
			{
				id = movieTape.Id,
			});

		}
		catch (Exception ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetMovieTapeById(int id)
	{
		var tape = await _db.MovieTapes
		.Where(mt => mt.Id == id)
		.Select(mt => new MovieTapeResponseDto
		{
			MovieId = mt.MovieId,
			TapeTitle = mt.TapeTitle,
			mediaType = mt.MediaType,
			VideoUrl = mt.VideoUrl,
			ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_original/{mt.ThumbnailUrl}",
		})
		.FirstOrDefaultAsync();

		if (tape == null)
			return NotFound(new { error = "Movie tape not found" });

		return Ok(tape);
	}

	[HttpGet("movie/{movieId:int}")]
	public async Task<IActionResult> GetMovieTapesByMovieId(int movieId)
	{
		var tapes = await _db.MovieTapes
		.Where(mt => mt.MovieId == movieId)
		.Select(mt => new MovieTapeResponseDto
		{
			MovieId = mt.MovieId,
			TapeTitle = mt.TapeTitle,
			mediaType = mt.MediaType,
			VideoUrl = mt.VideoUrl,
			ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_original/{mt.ThumbnailUrl}"
		})
		.ToListAsync();

		return Ok(tapes);
	}
}
