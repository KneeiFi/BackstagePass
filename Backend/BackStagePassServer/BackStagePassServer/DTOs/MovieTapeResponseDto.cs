using BackStagePassServer.Models;

namespace BackStagePassServer.DTOs;

public class MovieTapeResponseDto
{
	public int MovieId { get; set; }
	public string TapeTitle { get; set; }
	public MediaType mediaType { get; set; }
	public string VideoUrl { get; set; }
	public string ThumbnailUrl { get; set; }
}
