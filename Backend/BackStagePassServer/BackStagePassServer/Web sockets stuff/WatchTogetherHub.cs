using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace BackStagePassServer.Web_sockets_stuff;

public class WatchTogetherHub : Hub
{
	private static readonly Dictionary<string, HashSet<string>> Rooms = new();

	public async Task JoinRoom(string roomId)
	{
		await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

		lock (Rooms)
		{
			if (!Rooms.ContainsKey(roomId))
				Rooms[roomId] = new HashSet<string>();

			Rooms[roomId].Add(Context.ConnectionId);
		}
	}

	public async Task LeaveRoom(string roomId)
	{
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

		lock (Rooms)
		{
			if (Rooms.TryGetValue(roomId, out var connections))
			{
				connections.Remove(Context.ConnectionId);
				if (connections.Count == 0)
					Rooms.Remove(roomId);
			}
		}
	}

	public async Task SendCommand(string roomId, string command, object data)
	{
		// Отправить ВСЕМ участникам кроме отправителя
		//await Clients.GroupExcept(roomId, Context.ConnectionId).SendAsync("ReceiveCommand", command, data);

		// Отправить ВСЕМ участникам, включая отправителя
		await Clients.Group(roomId).SendAsync("ReceiveCommand", command, data);
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		// Очистка комнат по отключению — можно реализовать позже
		return base.OnDisconnectedAsync(exception);
	}
}
