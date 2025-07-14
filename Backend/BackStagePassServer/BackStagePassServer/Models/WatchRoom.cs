using System.ComponentModel.DataAnnotations;

namespace BackStagePassServer.Models;

public class WatchRoom
{
	public int Id { get; set; }

	[Required]
	public string RoomCode { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public bool IsPrivate { get; set; } = false;

	public string? PasswordHash { get; set; } // если хочешь позже добавить пароль

	public ICollection<WatchRoomUser> Users { get; set; } = new List<WatchRoomUser>();
}
