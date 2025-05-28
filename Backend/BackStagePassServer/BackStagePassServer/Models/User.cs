namespace BackStagePassServer.Models;

public enum UserRole { Admin, User }

public class User
{
	public int Id { get; set; }
	public UserRole Role { get; set; } = UserRole.User;
	public string Username { get; set; }
	public string Email { get; set; }
	public string PasswordHash { get; set; }
	public string AvatarUrl { get; set; } = "test";
	public int IsBanned { get; set; } = 0;
	public int IsEmailConfirmed { get; set; } = 0;

	public ICollection<UserToken> Tokens { get; set; }
}