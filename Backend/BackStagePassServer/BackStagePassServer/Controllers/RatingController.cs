using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;


[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _db;

	public RatingController(AuthService authService, AppDbContext db)
	{
		_authService = authService;
		_db = db;
	}


	/// <summary>
	/// new rating for a movie or update existing rating and recalculate movie rating overall
	[HttpPost("rate/{movieId:int}")]
	public async Task<IActionResult> RateMovie(int movieId, int rating, 
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before rating movies." });
		if (rating < 1 || rating > 10)
			return BadRequest(new { error = "Rating must be between 1 and 10." });
		var movie = await _db.Movies.FindAsync(movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var existingRating = await _db.Ratings
			.FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == user.Id);
		if (existingRating != null)
		{
			existingRating.Value = rating;
			_db.Ratings.Update(existingRating);
		}
		else
		{
			var newRating = new Rating
			{
				UserId = user.Id,
				MovieId = movieId,
				Value = rating,
			};
			await _db.Ratings.AddAsync(newRating);
		}
		await _db.SaveChangesAsync();

		// Calculate and update overall movie rating
		var ratings = await _db.Ratings
			.Where(r => r.MovieId == movieId)
			.ToListAsync();
		if (ratings.Count > 0)
		{
			float newAverage = (float)ratings.Sum(r => r.Value) / ratings.Count;
			movie.Rating = newAverage;
			_db.Movies.Update(movie);
			await _db.SaveChangesAsync();
		}

		return Ok(new { message = "Rating submitted successfully." });
	}


	

	// ... inside RatingController ...

	// Get all ratings for a specific movie
	[HttpGet("movie/{movieId:int}")]
	public async Task<IActionResult> GetRatingsByMovieId(int movieId)
	{
		var movie = await _db.Movies.FindAsync(movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var ratings = await _db.Ratings
			.Where(r => r.MovieId == movieId)
			.Select(r => new RatingDto
			{
				Id = r.Id,
				UserId = r.UserId,
				MovieId = r.MovieId,
				Value = r.Value
			})
			.ToListAsync();

		return Ok(ratings);
	}

	// Get all ratings by the current user (from access token)
	[HttpGet("user")]
	public async Task<IActionResult> GetRatingsByUserId([FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting ratings." });
		var ratings = await _db.Ratings
			.Where(r => r.UserId == user.Id)
			.Select(r => new RatingDto
			{
				Id = r.Id,
				UserId = r.UserId,
				MovieId = r.MovieId,
				Value = r.Value
			})
			.ToListAsync();

		return Ok(ratings);
	}

	// Get a rating by user and movie id (user from access token)
	[HttpGet("user/{movieId:int}")]
	public async Task<IActionResult> GetRatingByUserAndMovieId(int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting ratings." });
		var rating = await _db.Ratings
			.Where(r => r.MovieId == movieId && r.UserId == user.Id)
			.Select(r => new RatingDto
			{
				Id = r.Id,
				UserId = r.UserId,
				MovieId = r.MovieId,
				Value = r.Value
			})
			.FirstOrDefaultAsync();

		if (rating == null)
			return NotFound(new { error = "Rating not found." });

		return Ok(rating);
	}


	// Delete a rating by user and movie id (user from access token)
	[HttpDelete("user/movie/{movieId:int}")]
	public async Task<IActionResult> DeleteRatingByUserAndMovieId(int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		var rating = await _db.Ratings
			.FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == user.Id);

		if (rating == null)
			return NotFound(new { error = "Rating not found." });

		_db.Ratings.Remove(rating);
		await _db.SaveChangesAsync();

		// Recalculate movie rating
		var ratings = await _db.Ratings.Where(r => r.MovieId == movieId).ToListAsync();
		var movie = await _db.Movies.FindAsync(movieId);
		if (movie != null)
		{
			movie.Rating = ratings.Count > 0 ? (float)ratings.Sum(r => r.Value) / ratings.Count : 0;
			_db.Movies.Update(movie);
			await _db.SaveChangesAsync();
		}

		return Ok(new { message = "Rating deleted successfully." });
	}

	// Delete all ratings by user (admin only, userId sent in route)
	[HttpDelete("user/{userId:int}")]
	public async Task<IActionResult> DeleteRatingsByUserId(int userId, [FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });
		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting ratings." });
		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can delete all ratings" });

		var targetUser = await _db.Users.FindAsync(userId);
		if (targetUser == null)
			return NotFound(new { error = "User not found." });

		var ratings = await _db.Ratings.Where(r => r.UserId == userId).ToListAsync();
		if (!ratings.Any())
			return NotFound(new { error = "No ratings found for user." });

		_db.Ratings.RemoveRange(ratings);
		await _db.SaveChangesAsync();

		// Recalculate ratings for affected movies
		var movieIds = ratings.Select(r => r.MovieId).Distinct();
		foreach (var movieId in movieIds)
		{
			var movieRatings = await _db.Ratings.Where(r => r.MovieId == movieId).ToListAsync();
			var movie = await _db.Movies.FindAsync(movieId);
			if (movie != null)
			{
				movie.Rating = movieRatings.Count > 0 ? (float)movieRatings.Sum(r => r.Value) / movieRatings.Count : 0;
				_db.Movies.Update(movie);
			}
		}
		await _db.SaveChangesAsync();

		return Ok(new { message = "All ratings deleted for user." });
	}


}
