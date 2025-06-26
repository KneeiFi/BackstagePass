using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
	private readonly IPosterService _posterService;
	private readonly AuthService _authService;
	private readonly AppDbContext _db;

	public UserController(AuthService authService, AppDbContext db, IPosterService posterService)
	{
		_authService = authService;
		_db = db;
		_posterService = posterService;
	}

	[HttpPut("update")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateUser( [FromForm] UserUpdateDto dto,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		if (string.IsNullOrEmpty(accessToken))
			return Unauthorized(new { error = "Access token is missing." });

		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating user details." });

		if (dto.Username != null)
			user.Username = dto.Username;

		if (dto.Avatar != null)
		{
			if (user.AvatarUrl != null)
			{
				try
				{
					// Delete old avatar if it exists
					await _posterService.DeleteFileByNameAsync(user.AvatarUrl);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error deleting old avatar: {ex.Message}");
				}
			}
			user.AvatarUrl = await _posterService.SaveProfileAsync(dto.Avatar);
		}

		_db.Users.Update(user);
		await _db.SaveChangesAsync();

		return Ok(new { message = "User updated successfully." });
	}

	[HttpGet("by-movie/{movieId}")]
	public async Task<IActionResult> GetUserByMovieId(int movieId)
	{
		var movie = await _db.Movies.FindAsync(movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var user = await _db.Users.FindAsync(movie.UserId);
		if (user == null)
			return NotFound(new { error = "User not found." });

		var userDto = new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{user.AvatarUrl}"
		};

		return Ok(userDto);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetUserById(int id)
	{
		var user = await _db.Users.FindAsync(id);
		if (user == null)
			return NotFound(new { error = "User not found." });

		var userDto = new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{user.AvatarUrl}"
		};

		return Ok(userDto);
	}

	[HttpPost("ban/{id:int}")]
	public async Task<IActionResult> BanUserById(int id,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		if (string.IsNullOrEmpty(accessToken))
			return Unauthorized(new { error = "Access token is missing." });

		var adminUser = await _authService.GetUserByAccessToken(accessToken);
		if (adminUser == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (adminUser.Role == null || adminUser.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can ban users." });

		var user = await _db.Users.FindAsync(id);
		if (user == null)
			return NotFound(new { error = "User not found." });

		if (user.Role == UserRole.Admin)
			return BadRequest(new { error = "Cannot ban an admin user." });

		if (user.IsBanned == 1)
			return BadRequest(new { error = "User is already banned." });

		user.IsBanned = 1;
		_db.Users.Update(user);
		await _db.SaveChangesAsync();

		return Ok(new { message = "User has been banned successfully." });
	}

	[HttpPost("unban/{id:int}")]
	public async Task<IActionResult> UnbanUserById(int id,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		if (string.IsNullOrEmpty(accessToken))
			return Unauthorized(new { error = "Access token is missing." });

		var adminUser = await _authService.GetUserByAccessToken(accessToken);

		if (adminUser == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (adminUser.Role == null || adminUser.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can unban users." });

		var user = await _db.Users.FindAsync(id);
		if (user == null)
			return NotFound(new { error = "User not found." });

		if (user.IsBanned == 0)
			return BadRequest(new { error = "User is not banned." });

		user.IsBanned = 0;
		_db.Users.Update(user);
		await _db.SaveChangesAsync();
		return Ok(new { message = "User has been unbanned successfully." });
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetCurrentUser([FromHeader(Name = "Authorization")] string accessToken)
	{
		if (string.IsNullOrEmpty(accessToken))
			return Unauthorized(new { error = "Access token is missing." });
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });
		var userDto = new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{user.AvatarUrl}"
		};
		return Ok(userDto);
	}
}
