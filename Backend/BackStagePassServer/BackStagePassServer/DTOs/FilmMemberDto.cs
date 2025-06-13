namespace BackStagePassServer.DTOs;

public class FilmMemberDto
{
	public string Role { get; set; }
	public string FIO { get; set; }
	public IFormFile? Picture { get; set; }
}
