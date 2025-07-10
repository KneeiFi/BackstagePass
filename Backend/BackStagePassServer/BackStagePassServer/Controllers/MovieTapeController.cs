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
			return BadRequest(new { error = "Only admins can upload movie tapes." });
		if (request.Video == null)
			return BadRequest(new { error = "No file selected" });

		string fullVideoUrl = null;
		string thumbnailUrl = null;
		try
		{ 
			var relativePath = await _videoService.SaveVideoAsync(request.Video);

			fullVideoUrl  = $"{Request.Scheme}://{Request.Host}{relativePath}";

			thumbnailUrl = await _posterService.SavePosterAsync(request.Thumbnail);

			var movieTape = new MovieTape
			{
				TapeTitle = request.TapeTitle,
				MediaType = request.MediaType,
				VideoUrl = fullVideoUrl,
				ThumbnailUrl = thumbnailUrl,
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
			_videoService.DeleteVideoByUrlAsync(fullVideoUrl).Wait(); // Ensure cleanup on error
			_posterService.DeleteFileByNameAsync(thumbnailUrl).Wait(); // Ensure cleanup on error
			
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
			Id = mt.Id,
			MovieId = mt.MovieId,
			TapeTitle = mt.TapeTitle,
			mediaType = mt.MediaType,
			VideoUrl = mt.VideoUrl,
			ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_original/{mt.ThumbnailUrl}",
		})
		.FirstOrDefaultAsync();

		if (tape == null)
			return NoContent();

		return Ok(tape);
	}

	[HttpGet("movie/{movieId:int}")]
	public async Task<IActionResult> GetMovieTapesByMovieId(int movieId)
	{
		var tapes = await _db.MovieTapes
		.Where(mt => mt.MovieId == movieId)
		.Select(mt => new MovieTapeResponseDto
		{
			Id = mt.Id,
			MovieId = mt.MovieId,
			TapeTitle = mt.TapeTitle,
			mediaType = mt.MediaType,
			VideoUrl = mt.VideoUrl,
			ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_480p/{mt.ThumbnailUrl}"
		})
		.ToListAsync();

		return Ok(tapes);
	}

	[HttpGet("movie/by-exact-title/{movieTitle}")]
	public async Task<IActionResult> GetMovieTapesByMovieTitle(string movieTitle)
	{
		if (string.IsNullOrWhiteSpace(movieTitle))
			return BadRequest(new { error = "Movie title is required." });

		var tapes = await _db.MovieTapes
		.Where(mt => EF.Functions.Like(mt.TapeTitle, $"%{movieTitle}%"))
		.Select(mt => new MovieTapeResponseDto
		{
			Id = mt.Id,
			MovieId = mt.MovieId,
			TapeTitle = mt.TapeTitle,
			mediaType = mt.MediaType,
			VideoUrl = mt.VideoUrl,
			ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_480p/{mt.ThumbnailUrl}"
		})
		.ToListAsync();

		return Ok(tapes);
	}

	[HttpGet("movie/by-title/{movieTitle}")]
	public async Task<IActionResult> GetMovieTapesByMovieTitle(string movieTitle,[FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10)
	{
		if (string.IsNullOrWhiteSpace(movieTitle))
			return BadRequest(new { error = "Movie title is required." });

		if (pageNumber < 1 || pageSize < 1)
			return BadRequest(new { error = "Invalid page parameters." });

		var offset = (pageNumber - 1) * pageSize;

		var query = @"
        SELECT * FROM ""MovieTapes""
        WHERE similarity(""TapeTitle"", CAST({0} AS text)) > 0.01
        ORDER BY similarity(""TapeTitle"", CAST({0} AS text)) DESC
        OFFSET {1} LIMIT {2}";

		var tapes = await _db.MovieTapes
			.FromSqlRaw(query, movieTitle, offset, pageSize)
			.Select(mt => new MovieTapeResponseDto
			{
				Id = mt.Id,
				MovieId = mt.MovieId,
				TapeTitle = mt.TapeTitle,
				mediaType = mt.MediaType,
				VideoUrl = mt.VideoUrl,
				ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_480p/{mt.ThumbnailUrl}"
			})
			.ToListAsync();

		return Ok(tapes);
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> DeleteMovieTape(int id, 
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting a video." });
		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can delete movie tapes." });

		var tape = await _db.MovieTapes.Include(mt => mt.Movie).FirstOrDefaultAsync(mt => mt.Id == id);
		if (tape == null)
			return NotFound(new { error = "Movie tape not found" });

		
		await _videoService.DeleteVideoByUrlAsync(tape.VideoUrl);

		await _posterService.DeleteFileByNameAsync(tape.ThumbnailUrl);

		_db.MovieTapes.Remove(tape);
		await _db.SaveChangesAsync();

		return Ok(new { message = "Movie tape and related files deleted successfully." });
	}

	[HttpPut("{id:int}")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateMovieTape(int id,[FromForm] UpdateTapeDto request,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating a video." });
		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can update movie tapes." });

		var tape = await _db.MovieTapes.FirstOrDefaultAsync(mt => mt.Id == id);
		if (tape == null)
			return NotFound(new { error = "Movie tape not found" });

		// Update fields except video
		tape.TapeTitle = request.TapeTitle ?? tape.TapeTitle;
		tape.MovieId = request.MovieId ?? tape.MovieId;

		// Poster is optional
		if (request.Thumbnail != null)
		{
			// Delete old poster if exists
			if (!string.IsNullOrEmpty(tape.ThumbnailUrl))
			{
				await _posterService.DeleteFileByNameAsync(tape.ThumbnailUrl);
			}
			var newPosterName = await _posterService.SavePosterAsync(request.Thumbnail);
			tape.ThumbnailUrl = newPosterName;
		}

		await _db.SaveChangesAsync();

		return Ok(new { message = "Movie tape updated successfully." });
	}

}
