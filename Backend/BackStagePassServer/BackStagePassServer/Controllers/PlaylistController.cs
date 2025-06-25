
using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaylistController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _context;

	public PlaylistController(AppDbContext context, AuthService authService)
	{
		_context = context;
		_authService = authService;
	}

	// Add a movie to the user's "history" playlist.
	[HttpPost("history/{movieId:int}")]
	public async Task<IActionResult> AddToHistory(int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before adding to history." });

		var movie = await _context.Movies.FindAsync(movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		// Check if the movie is already in the "history" playlist
		var existingMovie = await _context.Playlists
		.FirstOrDefaultAsync(p => p.Title == "history" && p.UserId == user.Id && p.MovieId == movieId);

		if (existingMovie != null)
		{
			_context.Playlists.Remove(existingMovie);
			await _context.SaveChangesAsync();
		}

		var playlistMovie = new Playlist
		{
			UserId = user.Id,
			MovieId = movieId,
			Title = "history"
		};
		await _context.Playlists.AddAsync(playlistMovie);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Movie added to history." });

	}

	// Get the user's "history" playlist with pagination.
	[HttpGet("history")]
	public async Task<IActionResult> GetHistory([FromHeader(Name = "Authorization")] string accessToken,
		[FromQuery] int page = 1, [FromQuery] int pageSize = 30)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 30;

		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before viewing history." });

		var query = _context.Playlists
			.Where(p => p.Title == "history" && p.UserId == user.Id)
			.Include(p => p.Movie)
			.OrderByDescending(p => EF.Property<DateTime>(p, "CreatedAt"));

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new PlaylistHistoryItemDto
			{
				PlaylistId = p.Id,
				MovieId = p.MovieId,
				MovieTitle = p.Movie.Title,
				MovieRating = p.Movie.Rating,
				MoviePosterURL = $"{Request.Scheme}://{Request.Host}/posters_480p/{p.Movie.PosterURL}",
				AddedAt = p.CreatedAt
			})
			.ToListAsync();

		return Ok(new
		{
			page,
			pageSize,
			totalCount,
			items
		});
	}

	// Clear the user's "history" playlist.
	[HttpDelete("history")]
	public async Task<IActionResult> ClearHistory([FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before clearing history." });

		var historyItems = await _context.Playlists
			.Where(p => p.Title == "history" && p.UserId == user.Id)
			.ToListAsync();

		if (historyItems.Count == 0)
			return Ok(new { message = "History is already empty." });

		_context.Playlists.RemoveRange(historyItems);
		await _context.SaveChangesAsync();

		return Ok(new { message = "History cleared." });
	}



	// Add a movie to the user's "watch later" playlist.
	[HttpPost("watchlater/{movieId:int}")]
	public async Task<IActionResult> AddToWatchLater(int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before adding to watch later." });

		var movie = await _context.Movies.FindAsync(movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		// Check if the movie is already in the "watch later" playlist
		var existingMovie = await _context.Playlists
			.FirstOrDefaultAsync(p => p.Title == "watchlater" && p.UserId == user.Id && p.MovieId == movieId);

		if (existingMovie != null)
		{
			return Ok(new { message = "Movie already added to watch later." });
		}

		var playlistMovie = new Playlist
		{
			UserId = user.Id,
			MovieId = movieId,
			Title = "watchlater"
		};
		await _context.Playlists.AddAsync(playlistMovie);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Movie added to watch later." });
	}

	// Get the user's "watch later" playlist with pagination.
	[HttpGet("watchlater")]
	public async Task<IActionResult> GetWatchLater([FromHeader(Name = "Authorization")] string accessToken,
		[FromQuery] int page = 1, [FromQuery] int pageSize = 30)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 30;

		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before viewing watch later." });

		var query = _context.Playlists
			.Where(p => p.Title == "watchlater" && p.UserId == user.Id)
			.Include(p => p.Movie)
			.OrderByDescending(p => EF.Property<DateTime>(p, "CreatedAt"));

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new PlaylistHistoryItemDto
			{
				PlaylistId = p.Id,
				MovieId = p.MovieId,
				MovieTitle = p.Movie.Title,
				MovieRating = p.Movie.Rating,
				MoviePosterURL = $"{Request.Scheme}://{Request.Host}/posters_480p/{p.Movie.PosterURL}",
				AddedAt = p.CreatedAt
			})
			.ToListAsync();

		return Ok(new
		{
			page,
			pageSize,
			totalCount,
			items
		});
	}

	// Delete a movie from the user's "watch later" playlist by playlist id.
	[HttpDelete("watchlater/{playlistId:int}")]
	public async Task<IActionResult> DeleteFromWatchLater(int playlistId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before modifying watch later." });

		var playlistItem = await _context.Playlists
			.FirstOrDefaultAsync(p => p.Id == playlistId && p.Title == "watchlater" && p.UserId == user.Id);

		if (playlistItem == null)
			return NotFound(new { error = "Playlist item not found." });

		_context.Playlists.Remove(playlistItem);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Movie removed from watch later." });
	}



	// Create a new playlist or add a movie to an existing playlist
	[HttpPost("custom")]
	public async Task<IActionResult> CreateOrAddToPlaylist(
		[FromBody] PlaylistCreateOrAddDto dto,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before creating playlists." });

		if (string.IsNullOrWhiteSpace(dto.Title))
			return BadRequest(new { error = "Playlist title is required." });

		var movie = await _context.Movies.FindAsync(dto.MovieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		// Check if playlist exists for user
		var existingPlaylist = await _context.Playlists
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Title == dto.Title && p.MovieId == dto.MovieId);

		if (existingPlaylist != null)
			return Ok(new { message = "Movie already in this playlist." });

		// If playlist exists, get description from first item, else use provided
		var firstPlaylistItem = await _context.Playlists
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Title == dto.Title);

		var playlistMovie = new Playlist
		{
			UserId = user.Id,
			MovieId = dto.MovieId,
			Title = dto.Title,
			Description = firstPlaylistItem?.Description ?? dto.Description
		};

		await _context.Playlists.AddAsync(playlistMovie);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Movie added to playlist." });
	}

	// Update playlist title and description
	[HttpPut("custom/{playlistTitle}")]
	public async Task<IActionResult> UpdatePlaylist(
		string playlistTitle,
		[FromBody] PlaylistUpdateDto dto,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating playlists." });

		if (string.IsNullOrWhiteSpace(dto.Title))
			return BadRequest(new { error = "New playlist title is required." });

		var playlistItems = await _context.Playlists
			.Where(p => p.UserId == user.Id && p.Title == playlistTitle)
			.ToListAsync();

		if (!playlistItems.Any())
			return NotFound(new { error = "Playlist not found." });

		foreach (var item in playlistItems)
		{
			item.Title = dto.Title;
			item.Description = dto.Description;
		}

		await _context.SaveChangesAsync();
		return Ok(new { message = "Playlist updated." });
	}

	// Delete entire playlist by title
	[HttpDelete("custom/{playlistTitle}")]
	public async Task<IActionResult> DeletePlaylist(
		string playlistTitle,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting playlists." });

		var playlistItems = await _context.Playlists
			.Where(p => p.UserId == user.Id && p.Title == playlistTitle)
			.ToListAsync();

		if (!playlistItems.Any())
			return NotFound(new { error = "Playlist not found." });

		_context.Playlists.RemoveRange(playlistItems);
		await _context.SaveChangesAsync();
		return Ok(new { message = "Playlist deleted." });
	}

	// Delete a movie from a playlist, and delete playlist if empty
	[HttpDelete("custom/{playlistTitle}/movie/{movieId:int}")]
	public async Task<IActionResult> DeleteMovieFromPlaylist(
		string playlistTitle,
		int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before modifying playlists." });

		var playlistItem = await _context.Playlists
			.FirstOrDefaultAsync(p => p.UserId == user.Id && p.Title == playlistTitle && p.MovieId == movieId);

		if (playlistItem == null)
			return NotFound(new { error = "Movie not found in playlist." });

		_context.Playlists.Remove(playlistItem);
		await _context.SaveChangesAsync();

		// If no more movies in playlist, delete all (should be none left)
		var remaining = await _context.Playlists
			.AnyAsync(p => p.UserId == user.Id && p.Title == playlistTitle);

		if (!remaining)
			return Ok(new { message = "Movie removed. Playlist is now empty and deleted." });

		return Ok(new { message = "Movie removed from playlist." });
	}

	// Get all movies from a user's playlist by userId and playlist title, with pagination
	[HttpGet("custom")]
	public async Task<IActionResult> GetPlaylistMovies(
		[FromQuery] int userId,
		[FromQuery] string playlistTitle,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 30)
	{
		if (page < 1) page = 1;
		if (pageSize < 1) pageSize = 30;

		if (string.IsNullOrWhiteSpace(playlistTitle))
			return BadRequest(new { error = "Playlist title is required." });

		var query = _context.Playlists
			.Where(p => p.UserId == userId && p.Title == playlistTitle)
			.Include(p => p.Movie)
			.OrderByDescending(p => EF.Property<DateTime>(p, "CreatedAt"));

		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(p => new PlaylistHistoryItemDto
			{
				PlaylistId = p.Id,
				MovieId = p.MovieId,
				MovieTitle = p.Movie.Title,
				MovieRating = p.Movie.Rating,
				MoviePosterURL = $"{Request.Scheme}://{Request.Host}/posters_480p/{p.Movie.PosterURL}",
				AddedAt = p.CreatedAt
			})
			.ToListAsync();

		return Ok(new
		{
			page,
			pageSize,
			totalCount,
			items
		});
	}


	
	[HttpGet("all")]
	public async Task<IActionResult> GetAllPlaylists(
		[FromHeader(Name = "Authorization")] string accessToken,
		[FromQuery] int? userId = null)
	{
		int resolvedUserId;
		if (userId.HasValue)
		{
			resolvedUserId = userId.Value;
		}
		else
		{
			var user = await _authService.GetUserByAccessToken(accessToken);
			if (user == null)
				return Unauthorized(new { error = "Invalid access token" });
			if (user.Role == null)
				return BadRequest(new { error = "Email not confirmed. Please confirm your email before viewing playlists." });
			resolvedUserId = user.Id;
		}

		// Helper function to get playlist summary
		async Task<PlaylistSummaryDto> GetPlaylistSummaryAsync(string title)
		{
			var items = await _context.Playlists
				.Where(p => p.UserId == resolvedUserId && p.Title == title)
				.Include(p => p.Movie)
				.ToListAsync();

			if (!items.Any())
				return null;

			return new PlaylistSummaryDto
			{
				Title = title,
				Description = items.Select(x => x.Description).FirstOrDefault(d => d != null),
				MovieCount = items.Count,
				PosterURL = items
					.Where(x => x.Movie != null && x.Movie.PosterURL != null)
					.Select(x => $"{Request.Scheme}://{Request.Host}/posters_480p/{x.Movie.PosterURL}")
					.FirstOrDefault()
			};
		}

		// Get custom playlists
		var customPlaylists = await _context.Playlists
			.Where(p => p.UserId == resolvedUserId && p.Title != "history" && p.Title != "watchlater")
			.Include(p => p.Movie)
			.GroupBy(p => p.Title)
			.Select(g => new PlaylistSummaryDto
			{
				Title = g.Key,
				Description = g.Select(x => x.Description).FirstOrDefault(d => d != null),
				MovieCount = g.Count(),
				PosterURL = g
					.Where(x => x.Movie != null && x.Movie.PosterURL != null)
					.Select(x => $"{Request.Scheme}://{Request.Host}/posters_480p/{x.Movie.PosterURL}")
					.FirstOrDefault()
			})
			.ToListAsync();

		// Get history and watchlater playlists
		var history = await GetPlaylistSummaryAsync("history");
		var watchLater = await GetPlaylistSummaryAsync("watchlater");

		var allPlaylists = new List<PlaylistSummaryDto>();
		if (history != null) allPlaylists.Add(history);
		if (watchLater != null) allPlaylists.Add(watchLater);
		allPlaylists.AddRange(customPlaylists);

		var response = new AllPlaylistsResponseDto
		{
			Playlists = allPlaylists
		};

		return Ok(response);
	}

}