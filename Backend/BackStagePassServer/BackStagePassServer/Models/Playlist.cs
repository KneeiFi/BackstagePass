namespace BackStagePassServer.Models;

public class Playlist
{
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; }

	public int MovieId { get; set; }
	public Movie Movie { get; set; }

	public string Title { get; set; }
	public string? Description { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
