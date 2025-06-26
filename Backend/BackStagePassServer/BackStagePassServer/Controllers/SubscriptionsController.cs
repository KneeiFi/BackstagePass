using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _context;

	public SubscriptionsController(AuthService authService, AppDbContext context)
	{
		_authService = authService;
		_context = context;
	}

	// Subscribe to a user by their id.
	[HttpPost("subscribe/{targetUserId:int}")]
	public async Task<IActionResult> SubscribeToUser(int targetUserId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var subscriber = await _authService.GetUserByAccessToken(accessToken);
		if (subscriber == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (subscriber.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before subscribing." });

		if (subscriber.Id == targetUserId)
			return BadRequest(new { error = "You cannot subscribe to yourself." });

		var targetUser = await _context.Users.FindAsync(targetUserId);
		if (targetUser == null)
			return NotFound(new { error = "Target user not found." });

		// Check if already subscribed
		var existingSubscription = await _context.UserSubscriptions
			.FirstOrDefaultAsync(s => s.User1Id == subscriber.Id && s.User2Id == targetUserId);

		if (existingSubscription != null)
			return BadRequest(new { error = "Already subscribed to this user." });

		var subscription = new UserSubscription
		{
			User1Id = subscriber.Id,
			User2Id = targetUserId
		};

		await _context.UserSubscriptions.AddAsync(subscription);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Successfully subscribed to user." });
	}


	// Unsubscribe from a user by their id.
	[HttpDelete("unsubscribe/{targetUserId:int}")]
	public async Task<IActionResult> UnsubscribeFromUser(int targetUserId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var subscriber = await _authService.GetUserByAccessToken(accessToken);
		if (subscriber == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (subscriber.Id == targetUserId)
			return BadRequest(new { error = "You cannot unsubscribe from yourself." });

		var subscription = await _context.UserSubscriptions
			.FirstOrDefaultAsync(s => s.User1Id == subscriber.Id && s.User2Id == targetUserId);

		if (subscription == null)
			return NotFound(new { error = "Subscription not found." });

		_context.UserSubscriptions.Remove(subscription);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Successfully unsubscribed from user." });
	}

	// Get all users that you are subscribed to (with profile img and username), with pagination
	[HttpGet("subscribed-to")]
	public async Task<IActionResult> GetSubscribedToUsers(
		[FromHeader(Name = "Authorization")] string accessToken,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		var query = _context.UserSubscriptions
			.Where(s => s.User1Id == user.Id)
			.Select(s => s.User2);

		var total = await query.CountAsync();
		var users = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(u => new UserResponseDto
			{
				Id = u.Id,
				Username = u.Username,
				AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{u.AvatarUrl}"
			})
			.ToListAsync();

		return Ok(new
		{
			TotalCount = total,
			Page = page,
			PageSize = pageSize,
			Users = users
		});
	}

	// Get count of users that are subscribed to a user by id
	[HttpGet("subscribers/count/{userId:int}")]
	public async Task<IActionResult> GetSubscriberCount(int userId)
	{
		var count = await _context.UserSubscriptions.CountAsync(s => s.User2Id == userId);
		return Ok(new { count });
	}

	// Get all users that are subscribed to you (id from access token)
	[HttpGet("subscribers")]
	public async Task<IActionResult> GetSubscribers(
		[FromHeader(Name = "Authorization")] string accessToken,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		var query = _context.UserSubscriptions
			.Where(s => s.User2Id == user.Id)
			.Select(s => s.User1);

		var total = await query.CountAsync();
		var users = await query
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(u => new UserResponseDto
			{
				Id = u.Id,
				Username = u.Username,
				AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{u.AvatarUrl}"
			})
			.ToListAsync();

		return Ok(new
		{
			TotalCount = total,
			Page = page,
			PageSize = pageSize,
			Users = users
		});
	}

}
