namespace BackStagePassServer.Models;

public class FilmMember
{
	public int Id { get; set; }
	public string Role { get; set; }
	public string FIO { get; set; }
	public string? PictureUrl { get; set; }

	public ICollection<MovieFilmMember> MovieFilmMembers { get; set; }
}

