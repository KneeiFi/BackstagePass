using System.ComponentModel.DataAnnotations;

namespace BackStagePassServer.Models;

public class WatchRoomUser
{
	public int Id { get; set; }

	[Required]
	public string ConnectionId { get; set; } = default!;

	public int WatchRoomId { get; set; }
	public WatchRoom WatchRoom { get; set; } = default!;

	[Required]
	public string Role { get; set; } = "guest"; // "host", "guest", "moderator" и т.д.
}
