using BackStagePassServer.Models;

namespace BackStagePassServer.DTOs;

public class UserResponseDto
{
	public int Id { get; set; }
	public string Username { get; set; }
	public string AvatarUrl { get; set; }
}
