using System.ComponentModel.DataAnnotations;

namespace BackStagePassServer.Models;

public class WatchRoom
{
	public int Id { get; set; }

	[Required]
	public string RoomCode { get; set; } = default!; // Уникальный код комнаты (например, "test-room")

	public bool IsPrivate { get; set; } = false;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<WatchRoomUser> Users { get; set; } = new List<WatchRoomUser>();
}
