
namespace BackStagePassServer.Models;

public enum UserRole { User = 1, Admin }

public class User
{
	public int Id { get; set; }
	public UserRole? Role { get; set; } = null;
	public string Username { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public string AvatarUrl { get; set; } = "default";
	public int IsBanned { get; set; } = 0;

	public ICollection<Movie> Movies { get; set; }
	public ICollection<UserToken> Tokens { get; set; }
	public ICollection<Rating> Ratings { get; set; }
	public ICollection<Comment> Comments { get; set; }
	public ICollection<LikeComment> LikeComments { get; set; }
}