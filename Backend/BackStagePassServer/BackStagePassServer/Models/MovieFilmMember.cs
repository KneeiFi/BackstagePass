namespace BackStagePassServer.Models;

public class MovieFilmMember
{
	public int Id { get; set; }

	public int FilmMemberId { get; set; }
	public FilmMember FilmMember { get; set; }

	public int MovieId { get; set; }
	public Movie Movie { get; set; }
}
