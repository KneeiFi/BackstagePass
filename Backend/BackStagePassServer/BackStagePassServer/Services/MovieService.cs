using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using System.Text.Json;

namespace BackStagePassServer.Services;

public class MovieService : IMovieService
{
	private readonly AppDbContext _db;
	private readonly IPosterService _posterService;

	public MovieService(AppDbContext db, IPosterService posterService)
	{
		_db = db;
		_posterService = posterService;
	}

	public async Task<int> CreateMovieAndGenresAsync(UploadMovieWithGenresDto dto, User user, HttpRequest request)
	{
		var name = await _posterService.SavePosterAsync(dto.Poster);

		var movie = new Movie
		{
			Title = dto.Title,
			Description = dto.Description,
			Rating = dto.Rating,
			ReleaseDate = dto.ReleaseDate,
			PosterURL = name,
			UserId = user.Id,
			MovieGenres = new List<MovieGenre>(),
			MovieFilmMembers = new List<MovieFilmMember>()
		};

		// 1. Add new genres to context
		var genres = new List<Genre>();
		if (dto.Genres != null)
		{
			foreach (var genreName in dto.Genres)
			{
				var genre = _db.Genres.FirstOrDefault(g => g.Name == genreName);
				if (genre == null)
				{
					genre = new Genre { Name = genreName };
					_db.Genres.Add(genre);
				}
				genres.Add(genre);
			}
		}

		_db.Movies.Add(movie);
		await _db.SaveChangesAsync(); // IDs for new genres

		// 2. Add relationships
		foreach (var genre in genres)
		{
			movie.MovieGenres.Add(new MovieGenre
			{
				GenreId = genre.Id,
				MovieId = movie.Id
			});
		}

		await _db.SaveChangesAsync();

		return movie.Id;
	}
}
