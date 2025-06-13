namespace BackStagePassServer.DTOs;

public class FilmMemberResponseDto
{
	public int Id { get; set; }
	public string FIO { get; set; }
	public string Role { get; set; }
	public string? PictureUrl { get; set; }
}
