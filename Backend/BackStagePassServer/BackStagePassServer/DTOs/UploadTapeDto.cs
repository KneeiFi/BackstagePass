using BackStagePassServer.Models;

namespace BackStagePassServer.DTOs;

public class UploadTapeDto
{
	public int MovieId { get; set; }
	public string TapeTitle { get; set; }
	public MediaType MediaType { get; set; }
	public IFormFile Video { get; set; }
	public IFormFile Thumbnail { get; set; }
}
