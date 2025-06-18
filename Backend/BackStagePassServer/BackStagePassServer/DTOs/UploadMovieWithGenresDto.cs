namespace BackStagePassServer.DTOs;

public class UploadMovieWithGenresDto
{
	public string Title { get; set; }
	public string Description { get; set; }
	public float Rating { get; set; }
	public DateTime ReleaseDate { get; set; }
	public IFormFile Poster { get; set; }
	public List<string>? Genres { get; set; }
}
