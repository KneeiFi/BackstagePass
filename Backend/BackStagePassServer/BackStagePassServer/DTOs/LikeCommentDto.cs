namespace BackStagePassServer.DTOs
{
	public class LikeCommentDto
	{
		public int CommentId { get; set; }
		public int Type { get; set; } //  LikeType: 1 = Like, -1 = Dislike, 0 = Remove
	}
}
