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
	private readonly AuthService _authService;
	private readonly AppDbContext _db;
	public MovieController(AuthService authService, AppDbContext db, IMovieService movieService)
	{
		_authService = authService;
		_db = db;
		_movieService = movieService;
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
			return Forbid("Only admins can upload movies.");

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
}
