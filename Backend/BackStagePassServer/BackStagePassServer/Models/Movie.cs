namespace BackStagePassServer.Models;


public class Movie
{
	public int Id { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public float Rating { get; set; }
	public string PosterURL { get; set; }
	public DateTime ReleaseDate { get; set; }
	public int UserId { get; set; }
	public User User { get; set; }

	public ICollection<MovieTape> Tapes { get; set; }
	public ICollection<MovieGenre> MovieGenres { get; set; }
	public ICollection<MovieFilmMember> MovieFilmMembers { get; set; }
	public ICollection<Rating> Ratings { get; set; }
	public ICollection<Comment> Comments { get; set; }
}