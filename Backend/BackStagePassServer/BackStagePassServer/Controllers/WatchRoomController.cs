using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchRoomController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _db;

	public WatchRoomController(AuthService authService, AppDbContext db)
	{
		_authService = authService;
		_db = db;
	}

	[HttpGet("public-codes")]
	public async Task<ActionResult<object>> GetPublicRoomCodes()
	{
		var codes = await _db.WatchRooms
		.Where(r => !r.IsPrivate)
		.Select(r => r.RoomCode)
		.ToListAsync();

		return Ok(new { Codes = codes });
	}



	[HttpGet("exists/{code}")]
	public async Task<ActionResult<object>> RoomExists(string code)
	{
		var exists = await _db.WatchRooms
		.AnyAsync(r => r.RoomCode == code);

		return Ok(new { Exists = exists });
	}



	[HttpGet("user-ids/{code}")]
	public async Task<ActionResult<object>> GetUserIdsInRoom(string code)
	{
		var room = await _db.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == code);

		if (room == null)
			return NotFound(new { error = "Room not found" });

		var userIds = room.Users
			.Where(u => u.UserId != null)
			.Select(u => u.UserId)
			.ToList();

		return Ok(new { UserIds = userIds });
	}

}
