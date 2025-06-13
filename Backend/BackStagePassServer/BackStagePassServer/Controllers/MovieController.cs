using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MovieController : ControllerBase
{
	private readonly IMovieService _movieService;
	private readonly IVideoService _videoService;
	private readonly IPosterService _posterService;
	private readonly AuthService _authService;
	private readonly AppDbContext _db;
	public MovieController(AuthService authService, AppDbContext db, IMovieService movieService, IPosterService posterService, IVideoService videoService)
	{
		_authService = authService;
		_db = db;
		_movieService = movieService;
		_posterService = posterService;
		_videoService = videoService;
	}

	[HttpPost("upload/Movie&Genre")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UploadMovie([FromForm] UploadMovieWithGenresDto dto,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before uploading videos." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can upload movies." });

		try
		{
			var movieId = await _movieService.CreateMovieAndGenresAsync(dto, user, Request);
			return Ok(new
			{
				Id = movieId
			});
		}
		catch (Exception ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetMovieById(int id)
	{
		var movie = await _db.Movies
			.Where(m => m.Id == id)
			.Select(m => new
			{
				m.Id,
				m.Title,
				m.Description,
				m.Rating,
				m.ReleaseDate,
				PosterURL = $"{Request.Scheme}://{Request.Host}/posters_original/{m.PosterURL}",
				Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList(),

				FilmMembers = m.MovieFilmMembers.Select(fm => new
				{
					fm.FilmMember.Id,
					fm.FilmMember.FIO,
					fm.FilmMember.Role
				}).ToList(),

				Tapes = m.Tapes.Select(t => new 
				{ 
					t.Id,
					t.TapeTitle,
					t.MediaType,
					ThumbnailUrl = $"{Request.Scheme}://{Request.Host}/posters_480p/{t.ThumbnailUrl}"
				})
				.ToList()
			})
			.FirstOrDefaultAsync();

		if (movie == null)
		{
			return NotFound();
		}

		return Ok(movie);
		
	}

	[HttpGet("{id:int}/simple")]
	public async Task<IActionResult> GetMovieByIdSimple(int id)
	{
		var movie = await _db.Movies
		.Select(m => new MovieSimpleDto
		{
			Id = m.Id,
			Title = m.Title,
			Description = m.Description,
			Rating = m.Rating,
			ReleaseDate = m.ReleaseDate,
			PosterURL = $"{Request.Scheme}://{Request.Host}/posters_original/{m.PosterURL}"
		})
		.FirstOrDefaultAsync(m => m.Id == id);

		if (movie == null)
			return NotFound();

		return Ok(movie);
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAllMovies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 10;

		var query = _db.Movies
			.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre);

		var totalCount = await query.CountAsync();
		var movies = await query
			.OrderBy(m => m.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(m => new MovieListDto
			{
				Id = m.Id,
				Title = m.Title,
				Rating = m.Rating,
				ReleaseDate = m.ReleaseDate,
				PosterURL = $"{Request.Scheme}://{Request.Host}/posters_480p/{m.PosterURL}",
				Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList(),
			})
		   .ToListAsync();

		return Ok(new
		{
			TotalCount = totalCount,
			Page = page,
			PageSize = pageSize,
			Movies = movies
		});
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> DeleteMovie(int id,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting movies." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can delete movies." });

		var movie = await _db.Movies
			.Include(m => m.MovieGenres)
			.Include(m => m.MovieFilmMembers)
			.Include(m => m.Tapes)
			.FirstOrDefaultAsync(m => m.Id == id);

		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		
		// Remove related MovieGenres
		_db.MovieGenres.RemoveRange(movie.MovieGenres);

		// Remove related MovieFilmMembers
		_db.MovieFilmMembers.RemoveRange(movie.MovieFilmMembers);

		// Before removing tapes from the database
		foreach (var tape in movie.Tapes)
		{
			// Delete video file if exists
			if (!string.IsNullOrEmpty(tape.VideoUrl))
			{
				await _videoService.DeleteVideoByUrlAsync(tape.VideoUrl);
			}
			// Delete thumbnail/poster file if exists
			if (!string.IsNullOrEmpty(tape.ThumbnailUrl))
			{
				await _posterService.DeleteFileByNameAsync(tape.ThumbnailUrl);
			}
		}

		// Remove related Tapes
		_db.MovieTapes.RemoveRange(movie.Tapes);

		// Remove Ratings
		var ratings = _db.Set<Rating>().Where(r => r.MovieId == id);
		_db.Set<Rating>().RemoveRange(ratings);

		if (movie.PosterURL != null)
		{
			// Delete poster files
			await _posterService.DeleteFileByNameAsync(movie.PosterURL);
		}

		// Remove the movie itself
		_db.Movies.Remove(movie);

		await _db.SaveChangesAsync();

		return Ok(new { message = "Movie and all related data deleted successfully." });
	}

	[HttpPut("{id:int}/simple")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateMovieSimple(int id, [FromForm] MovieSimpleDtoUpdate dto,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating movies." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can update movies." });

		var movie = await _db.Movies.FirstOrDefaultAsync(m => m.Id == id);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		if (dto.Title != null)
			movie.Title = dto.Title;
		if (dto.Description != null)
			movie.Description = dto.Description;
		if (dto.Rating.HasValue)
			movie.Rating = dto.Rating.Value;
		if (dto.ReleaseDate.HasValue)
			movie.ReleaseDate = dto.ReleaseDate.Value;
		if (dto.Poster != null)
		{
			// Delete old poster if exists
			if (!string.IsNullOrEmpty(movie.PosterURL))
			{
				await _posterService.DeleteFileByNameAsync(movie.PosterURL);
			}
			// Save new poster and update PosterURL
			movie.PosterURL = await _posterService.SavePosterAsync(dto.Poster);
		}

		await _db.SaveChangesAsync();

		return Ok(new { message = "Movie updated successfully." });
	}
}
