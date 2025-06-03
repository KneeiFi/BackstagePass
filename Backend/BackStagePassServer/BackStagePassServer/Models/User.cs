namespace BackStagePassServer.Models;

public enum UserRole { Admin, User }

public class User
{
	public int Id { get; set; }
	public UserRole? Role { get; set; } = null;
	public string Username { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public string AvatarUrl { get; set; } = "default";
	public int IsBanned { get; set; } = 0;
	

	public ICollection<UserToken> Tokens { get; set; }
}