namespace BackStagePassServer.Models;

public class UserToken
{
	public int Id { get; set; }
	public string AccessToken { get; set; }
	public DateTime AccessTokenExpiryTime { get; set; }

	public string RefreshToken { get; set; }
	public DateTime RefreshTokenExpiryTime { get; set; }

	public int UserId { get; set; }
	public User User { get; set; }
}