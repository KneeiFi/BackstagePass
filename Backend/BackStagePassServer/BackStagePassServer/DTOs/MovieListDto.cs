namespace BackStagePassServer.DTOs;

public class MovieListDto
{
	public int Id { get; set; }
	public string Title { get; set; }
	public float Rating { get; set; }
	public DateTime ReleaseDate { get; set; }
	public string PosterURL { get; set; }
	public List<string> Genres { get; set; }
}
