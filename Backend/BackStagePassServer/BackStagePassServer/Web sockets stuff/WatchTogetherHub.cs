using BackStagePassServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BackStagePassServer.Web_sockets_stuff;

public class WatchTogetherHub : Hub
{
	private static readonly Dictionary<string, HashSet<string>> Rooms = new();

	private readonly AppDbContext _context;

	public WatchTogetherHub(AppDbContext context)
	{
		_context = context;
	}

	public async Task JoinRoom(string roomCode)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

		var room = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == roomCode);

		if (room == null)
		{
			room = new WatchRoom
			{
				RoomCode = roomCode,
				CreatedAt = DateTime.UtcNow,
				IsPrivate = false
			};

			_context.WatchRooms.Add(room);
			await _context.SaveChangesAsync();
		}

		// Определяем роль: первый — host
		var role = room.Users.Any() ? "guest" : "host";

		var user = new WatchRoomUser
		{
			ConnectionId = Context.ConnectionId,
			WatchRoomId = room.Id,
			Role = role
		};

		_context.WatchRoomUsers.Add(user);
		await _context.SaveChangesAsync();
	}

	public async Task LeaveRoom(string roomCode)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

		var room = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == roomCode);

		if (room == null)
			return;

		var user = room.Users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
		if (user != null)
		{
			bool wasHost = user.Role == "host";
			_context.WatchRoomUsers.Remove(user);
			await _context.SaveChangesAsync();

			// If the user was host and there are still users, assign host to the next user
			if (wasHost)
			{
				// Reload users after removal
				var updatedRoom = await _context.WatchRooms
					.Include(r => r.Users)
					.FirstOrDefaultAsync(r => r.Id == room.Id);

				var nextUser = updatedRoom?.Users.FirstOrDefault();
				if (nextUser != null)
				{
					nextUser.Role = "host";
					await _context.SaveChangesAsync();

					await Clients.Client(nextUser.ConnectionId)
					.SendAsync("ReceiveCommand", "set_role", new { role = "host" });

				}
			}
		}

		// If no users left, delete the room
		room = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == roomCode);

		if (room != null && room.Users.Count == 0)
		{
			_context.WatchRooms.Remove(room);
			await _context.SaveChangesAsync();
		}
	}

	public async Task SendCommand(string roomCode, string command, object data)
	{
		var room = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == roomCode);

		var user = room?.Users.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);
		if (user == null)
			return;

		// Специальная команда — запрос роли
		if (command == "get_role")
		{
			await Clients.Client(Context.ConnectionId)
				.SendAsync("ReceiveCommand", "set_role", new { role = user.Role });
			return;
		}

		// Ограничение: только host может отправлять команды
		if (user.Role != "host")
			return;

		// Отправить ВСЕМ КРОМЕ отправителя
		await Clients.GroupExcept(roomCode, Context.ConnectionId)
			.SendAsync("ReceiveCommand", command, data);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		var user = await _context.WatchRoomUsers
		.Include(u => u.WatchRoom)
		.ThenInclude(r => r.Users)
		.FirstOrDefaultAsync(u => u.ConnectionId == Context.ConnectionId);

		if (user == null)
			return;

		var room = user.WatchRoom;

		_context.WatchRoomUsers.Remove(user);
		await _context.SaveChangesAsync();

		// Заново загружаем комнату с актуальными пользователями
		var updatedRoom = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.Id == room.Id);

		if (updatedRoom == null)
			return;

		// Если больше нет пользователей — удалить комнату
		if (updatedRoom.Users.Count == 0)
		{
			_context.WatchRooms.Remove(updatedRoom);
			await _context.SaveChangesAsync();
		}
		else if (user.Role == "host")
		{
			// Назначить нового хоста
			var next = updatedRoom.Users.FirstOrDefault();
			if (next != null)
			{
				next.Role = "host";
				await _context.SaveChangesAsync();

				await Clients.Client(next.ConnectionId)
				.SendAsync("ReceiveCommand", "set_role", new { role = "host" });
			}
		}

		await base.OnDisconnectedAsync(exception);
	}
}






// Отправить ВСЕМ участникам кроме отправителя
//await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("ReceiveCommand", command, data);