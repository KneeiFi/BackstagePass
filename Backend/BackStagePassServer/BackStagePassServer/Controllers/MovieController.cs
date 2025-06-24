using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
			return NoContent();
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
			return NoContent();

		return Ok(movie);
	}

	[HttpGet("all")]
	public async Task<IActionResult> GetAllMoviesOrderByRating([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 10;

		var query = _db.Movies
			.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre);

		var totalCount = await query.CountAsync();
		var movies = await query
			.OrderByDescending(m => m.Rating)
			.ThenBy(m => m.Id)
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

	[HttpGet("search/{title}")]
	public async Task<IActionResult> SearchMoviesByTitle(string title,[FromQuery] int page = 1,[FromQuery] int pageSize = 10)
	{
		if (string.IsNullOrWhiteSpace(title))
			return BadRequest(new { error = "Title is required." });

		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 10;

		var offset = (page - 1) * pageSize;

		
		int totalCount;

		await using (var connection = new NpgsqlConnection(_db.Database.GetConnectionString()))
		{
			await connection.OpenAsync();

			await using (var command = connection.CreateCommand())
			{
				command.CommandText = @"SELECT COUNT(*) FROM ""Movies"" WHERE similarity(""Title"", @title) > 0.01";
				var param = command.CreateParameter();
				param.ParameterName = "@title";
				param.Value = title;
				command.Parameters.Add(param);

				var result = await command.ExecuteScalarAsync();
				totalCount = Convert.ToInt32(result);
			}
		}

		// SQL-запрос на выборку
		var rawSql = @"
        SELECT * FROM ""Movies""
        WHERE similarity(""Title"", CAST({0} AS text)) > 0.01
        ORDER BY similarity(""Title"", CAST({0} AS text)) DESC
        OFFSET {1} LIMIT {2}";

		// Получаем фильмы с жанрами
		var matchedMovies = await _db.Movies
			.FromSqlRaw(rawSql, title, offset, pageSize)
			.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
			.ToListAsync();

		var movies = matchedMovies.Select(m => new MovieListDto
		{
			Id = m.Id,
			Title = m.Title,
			Rating = m.Rating,
			ReleaseDate = m.ReleaseDate,
			PosterURL = $"{Request.Scheme}://{Request.Host}/posters_480p/{m.PosterURL}",
			Genres = m.MovieGenres.Select(g => g.Genre.Name).ToList(),
		}).ToList();

		return Ok(new
		{
			TotalCount = totalCount,
			Page = page,
			PageSize = pageSize,
			Movies = movies
		});
	}

	[HttpGet("search/genre/{genreName}")]
	public async Task<IActionResult> SearchMoviesByGenre(string genreName, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (string.IsNullOrWhiteSpace(genreName))
			return BadRequest(new { error = "Genre name is required." });

		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 10;

		var query = _db.Movies
		.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
		.Where(m => m.MovieGenres.Any(mg => mg.Genre.Name.ToLower() == genreName.ToLower()));

		var totalCount = await query.CountAsync();

		var movies = await query
		.OrderByDescending(m => m.Rating)
		.ThenBy(m => m.Id)
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

	[HttpGet("search/film-member/{fio}")]
	public async Task<IActionResult> SearchMoviesByFilmMemberFio(string fio, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (string.IsNullOrWhiteSpace(fio))
			return BadRequest(new { error = "Film member FIO is required." });

		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 10;

		var query = _db.Movies
		.Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
		.Include(m => m.MovieFilmMembers).ThenInclude(mfm => mfm.FilmMember)
		.Where(m => m.MovieFilmMembers.Any(mfm => mfm.FilmMember.FIO.ToLower().Contains(fio.ToLower())));

		var totalCount = await query.CountAsync();

		var movies = await query
		.OrderByDescending(m => m.Rating)
		.ThenBy(m => m.Id)
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

		// Remove Comments and LikeComments
		var comments = _db.Comments.Where(c => c.MovieId == id).ToList();
		var commentIds = comments.Select(c => c.Id).ToList();
		var likeComments = _db.LikeComments.Where(lc => commentIds.Contains(lc.CommentId)).ToList();
		_db.LikeComments.RemoveRange(likeComments);
		_db.Comments.RemoveRange(comments);

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
