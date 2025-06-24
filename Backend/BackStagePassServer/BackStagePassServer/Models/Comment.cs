namespace BackStagePassServer.Models;

public class Comment
{
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; }

	public int MovieId { get; set; }
	public Movie Movie { get; set; }

	public string Content { get; set; }

	public ICollection<LikeComment> LikeComments { get; set; }
}
