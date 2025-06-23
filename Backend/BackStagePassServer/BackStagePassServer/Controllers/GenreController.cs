using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenreController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _context;

	public GenreController(AppDbContext context, AuthService authService)
	{
		_context = context;
		_authService = authService;
	}

	// Get all genres
	[HttpGet("all")]
	public async Task<IActionResult> GetAllGenres()
	{
		var genres = await _context.Genres
			.Select(g => new GenreDto { Id = g.Id, Name = g.Name })
			.ToListAsync();
		return Ok(genres);
	}

	// Get all genres by movie id
	[HttpGet("by-movie/{movieId:int}")]
	public async Task<IActionResult> GetGenresByMovieId(int movieId)
	{
		var movie = await _context.Movies
			.Include(m => m.MovieGenres)
			.ThenInclude(mg => mg.Genre)
			.FirstOrDefaultAsync(m => m.Id == movieId);

		if (movie == null)
			return NoContent();

		var genres = movie.MovieGenres
			.Select(mg => new GenreDto 
			{
				Id = mg.Genre.Id,
				Name = mg.Genre.Name
			})
			.ToList();

		return Ok(genres);
	}

	// Get genre by name (first match)
	[HttpGet("by-name/{name}")]
	public async Task<IActionResult> GetGenreByName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { error = "Genre name is required." });

		var genre = await _context.Genres
			.Where(g => g.Name == name)
			.Select(g => new GenreDto 
			{
				Id = g.Id,
				Name = g.Name
			})
			.FirstOrDefaultAsync();

		if (genre == null)
			return NoContent();

		return Ok(genre);
	}

	// Get genre by id
	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetGenreById(int id)
	{
		var genre = await _context.Genres
			.Where(g => g.Id == id)
			.Select(g => new GenreDto 
			{
				Id = g.Id,
				Name = g.Name
			})
			.FirstOrDefaultAsync();

		if (genre == null)
			return NoContent();

		return Ok(genre);
	}

	// Add new genre
	[HttpPost("add")]
	public async Task<IActionResult> AddGenre([FromBody] string name,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before managing genres." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can manage genres." });

		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { error = "Genre name is required." });

		var exists = await _context.Genres.AnyAsync(g => g.Name == name);
		if (exists)
			return Conflict(new { error = "A genre with this name already exists." });

		var genre = new Genre { Name = name };
		_context.Genres.Add(genre);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Genre added successfully.", genre });
	}

	// Add genre to a film (create genre if not exists)
	[HttpPost("add-to-movie/{movieId:int}")]
	public async Task<IActionResult> AddGenreToMovie(int movieId,[FromBody] string genreName,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before managing genres." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can manage genres." });

		if (string.IsNullOrWhiteSpace(genreName))
			return BadRequest(new { error = "Genre name is required." });

		var movie = await _context.Movies.Include(m => m.MovieGenres).FirstOrDefaultAsync(m => m.Id == movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == genreName);
		if (genre == null)
		{
			genre = new Genre { Name = genreName };
			_context.Genres.Add(genre);
			await _context.SaveChangesAsync();
		}

		var alreadyLinked = await _context.MovieGenres.AnyAsync(mg => mg.MovieId == movieId && mg.GenreId == genre.Id);
		if (alreadyLinked)
			return Conflict(new { error = "This genre is already linked to the movie." });

		var movieGenre = new MovieGenre { MovieId = movieId, GenreId = genre.Id };
		_context.MovieGenres.Add(movieGenre);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Genre added to movie successfully." });
	}

	// Update genre
	[HttpPut("update/{id}")]
	public async Task<IActionResult> UpdateGenre(int id,[FromBody] string newName,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before managing genres." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can manage genres." });

		if (string.IsNullOrWhiteSpace(newName))
			return BadRequest(new { error = "New genre name is required." });

		var genre = await _context.Genres.FindAsync(id);
		if (genre == null)
			return NotFound(new { error = "Genre not found." });

		var exists = await _context.Genres.AnyAsync(g => g.Name == newName && g.Id != id);
		if (exists)
			return Conflict(new { error = "A genre with this name already exists." });

		genre.Name = newName;
		await _context.SaveChangesAsync();
		return Ok(new { message = "Genre updated successfully."});
	}

	// Delete genre from genres and all MovieGenre
	[HttpDelete("delete/{id}")]
	public async Task<IActionResult> DeleteGenre(
		int id,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before managing genres." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can manage genres." });

		var genre = await _context.Genres.Include(g => g.MovieGenres).FirstOrDefaultAsync(g => g.Id == id);
		if (genre == null)
			return NotFound(new { error = "Genre not found." });

		// Remove all MovieGenre links
		var movieGenres = _context.MovieGenres.Where(mg => mg.GenreId == id);
		_context.MovieGenres.RemoveRange(movieGenres);

		_context.Genres.Remove(genre);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Genre and all related links deleted." });
	}

}
