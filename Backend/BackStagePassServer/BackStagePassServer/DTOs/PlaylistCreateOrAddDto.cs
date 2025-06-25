namespace BackStagePassServer.DTOs;

public class PlaylistCreateOrAddDto
{
	public string Title { get; set; }
	public string? Description { get; set; }
	public int MovieId { get; set; }
}

