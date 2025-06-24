namespace BackStagePassServer.Models;

public class LikeComment
{
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; }

	public int CommentId { get; set; }
	public Comment Comment { get; set; }

	public int Type { get; set; } //  LikeType: 1 = Like, -1 = Dislike, 0 = Remove
}
