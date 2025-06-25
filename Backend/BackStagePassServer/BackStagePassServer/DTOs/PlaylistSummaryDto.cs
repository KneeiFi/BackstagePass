namespace BackStagePassServer.DTOs;

// DTO for playlist summary
public class PlaylistSummaryDto
{
	public string Title { get; set; }
	public string? Description { get; set; }
	public int MovieCount { get; set; }
	public string? PosterURL { get; set; }
}
