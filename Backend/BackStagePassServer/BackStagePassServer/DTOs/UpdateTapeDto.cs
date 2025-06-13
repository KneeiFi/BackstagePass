namespace BackStagePassServer.DTOs;

public class UpdateTapeDto
{
	public int? MovieId { get; set; }
	public string? TapeTitle { get; set; }
	public IFormFile? Thumbnail { get; set; }
}
