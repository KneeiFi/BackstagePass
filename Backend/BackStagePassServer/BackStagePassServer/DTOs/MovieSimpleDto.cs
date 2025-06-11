namespace BackStagePassServer.DTOs;

public class MovieSimpleDto
{
	public int Id { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public float Rating { get; set; }
	public DateTime ReleaseDate { get; set; }
	public string PosterURL { get; set; }
}
