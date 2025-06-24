namespace BackStagePassServer.DTOs;

public class CommentResponseDto
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string Username { get; set; }
	public string Content { get; set; }
	public int Likes { get; set; }
	public int Dislikes { get; set; }
	public string AvatarUrl { get; set; } 
}
