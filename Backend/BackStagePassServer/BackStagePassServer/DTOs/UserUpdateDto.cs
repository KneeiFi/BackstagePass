namespace BackStagePassServer.DTOs;

public class UserUpdateDto
{
	public string? Username { get; set; }
	public IFormFile? Avatar { get; set; }
}
