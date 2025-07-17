using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace BackStagePassServer.Web_sockets_stuff;

public class WatchTogetherHub : Hub
{
	private readonly AppDbContext _context;
	private readonly AuthService _authService;

	public WatchTogetherHub(AppDbContext context, AuthService authService)
	{
		_context = context;
		_authService = authService;
	}

	public async Task JoinRoom(string roomCode, string? password = null)
	{
		var httpContext = Context.GetHttpContext();
		if (httpContext == null)
		{
			Context.Abort();
			return;
		}

		// Чтение токена из query string вместо заголовка
		string? accessToken = httpContext.Request.Query["access_token"];
		if (string.IsNullOrWhiteSpace(accessToken))
		{
			await Clients.Caller.SendAsync("ReceiveCommand", "unauthorized", new { message = "Missing access token" });
			Context.Abort();
			return;
		}

		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
		{
			await Clients.Caller.SendAsync("ReceiveCommand", "unauthorized", new { message = "Invalid or expired access token" });
			Context.Abort();
			return;
		}


		await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

		var room = await _context.WatchRooms
			.Include(r => r.Users)
			.FirstOrDefaultAsync(r => r.RoomCode == roomCode);

		if (room != null)
		{
			// Если комната приватная — нужно проверить пароль
			if (room.IsPrivate)
			{
				if (string.IsNullOrWhiteSpace(password))
				{
					await Clients.Caller.SendAsync("ReceiveCommand", "unauthorized", new { message = "Password required" });
					Context.Abort();
					return;
				}

				if (!BCrypt.Net.BCrypt.Verify(password, room.PasswordHash))
				{
					await Clients.Caller.SendAsync("ReceiveCommand", "unauthorized", new { message = "Incorrect password" });
					Context.Abort();
					return;
				}
			}
		}
		else
		{
			// Если создаём новую комнату и был передан пароль — делаем её приватной
			room = new WatchRoom
			{
				RoomCode = roomCode,
				CreatedAt = DateTime.UtcNow,
				IsPrivate = !string.IsNullOrWhiteSpace(password),
				PasswordHash = string.IsNullOrWhiteSpace(password) ? null : BCrypt.Net.BCrypt.HashPassword(password)
			};

			_context.WatchRooms.Add(room);
			await _context.SaveChangesAsync();
		}

		// Определяем роль: первый — host
		var role = room.Users.Any() ? "guest" : "host";

		var userEntry = new WatchRoomUser
		{
			ConnectionId = Context.ConnectionId,
			WatchRoomId = room.Id,
			Role = role,
			UserId = user.Id
		};

		_context.WatchRoomUsers.Add(userEntry);
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

		if (command == "get_role")
		{
			await Clients.Client(Context.ConnectionId)
				.SendAsync("ReceiveCommand", "set_role", new { role = user.Role });
			return;
		}

		switch (command)
		{
			case "transfer_host":
			{

				if (user.Role != "host")
					return;

					// Ожидается: { userId: 123 }
					int? userId = (data as dynamic)?.userId;
				if (userId == null) return;

				var targetUser = room.Users.FirstOrDefault(u => u.UserId == userId);
				if (targetUser == null || targetUser.ConnectionId == Context.ConnectionId)
					return;

				user.Role = "guest";
				targetUser.Role = "host";
				await _context.SaveChangesAsync();

				await Clients.Client(targetUser.ConnectionId)
					.SendAsync("ReceiveCommand", "set_role", new { role = "host" });

				await Clients.Client(Context.ConnectionId)
					.SendAsync("ReceiveCommand", "set_role", new { role = "guest" });
				break;
			}

			case "kick":
			{
				if (user.Role != "host")
					return;

					// Ожидается: { userId: 123 }
					int? userId = (data as dynamic)?.userId;
				if (userId == null) return;

				var kickedUser = room.Users.FirstOrDefault(u => u.UserId == userId);
				if (kickedUser == null || kickedUser.ConnectionId == Context.ConnectionId)
					return;

				_context.WatchRoomUsers.Remove(kickedUser);
				await _context.SaveChangesAsync();

				await Clients.Client(kickedUser.ConnectionId)
					.SendAsync("ReceiveCommand", "kicked", new { message = "You were kicked by the host" });

				await Groups.RemoveFromGroupAsync(kickedUser.ConnectionId, roomCode);
				break;
			}

			case "set_password":
			{
				if (user.Role != "host")
					return;

				string? password = (data as dynamic)?.password;
				room.IsPrivate = !string.IsNullOrEmpty(password);
				room.PasswordHash = string.IsNullOrEmpty(password) ? null : BCrypt.Net.BCrypt.HashPassword(password);
				await _context.SaveChangesAsync();

				await Clients.Caller.SendAsync("ReceiveCommand", "password_updated", new { success = true });
				break;
			}

			default:
			{
				await Clients.GroupExcept(roomCode, Context.ConnectionId)
					.SendAsync("ReceiveCommand", command, data);
				break;
			}
		}
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