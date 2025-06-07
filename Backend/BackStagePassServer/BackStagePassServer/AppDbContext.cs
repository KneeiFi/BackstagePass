using BackStagePassServer.Models;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer;

public class AppDbContext : DbContext
{
	public DbSet<EmailConfirm> EmailConfirms { get; set; }
	public DbSet<RessetPassword> RessetPasswords { get; set; }
	public DbSet<User> Users { get; set; }
	public DbSet<UserToken> UserTokens { get; set; }
	public DbSet<Movie> Movies { get; set; }
	public DbSet<MovieTape> MovieTapes { get; set; }
	public DbSet<Genre> Genres { get; set; }
	public DbSet<MovieGenre> MovieGenres { get; set; }
	public DbSet<FilmMember> FilmMembers { get; set; }
	public DbSet<MovieFilmMember> MovieFilmMembers { get; set; }
	public DbSet<Rating> Ratings { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// User: unique index on Email
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();

		// UserToken: зв’язок User -> UserToken
		modelBuilder.Entity<UserToken>()
			.HasOne(t => t.User)
			.WithMany(u => u.Tokens)
			.HasForeignKey(t => t.UserId);

		// many-to-many: Movie <-> Genre
		modelBuilder.Entity<MovieGenre>()
			.HasOne(mg => mg.Movie)
			.WithMany(m => m.MovieGenres)
			.HasForeignKey(mg => mg.MovieId);

		modelBuilder.Entity<MovieGenre>()
			.HasOne(mg => mg.Genre)
			.WithMany(g => g.MovieGenres)
			.HasForeignKey(mg => mg.GenreId);

		// many-to-many: Movie <-> FilmMember
		modelBuilder.Entity<MovieFilmMember>()
			.HasOne(mfm => mfm.Movie)
			.WithMany(m => m.MovieFilmMembers)
			.HasForeignKey(mfm => mfm.MovieId);

		modelBuilder.Entity<MovieFilmMember>()
			.HasOne(mfm => mfm.FilmMember)
			.WithMany(fm => fm.MovieFilmMembers)
			.HasForeignKey(mfm => mfm.FilmMemberId);

		// Rating: зв’язок User -> Rating -> Movie
		modelBuilder.Entity<Rating>()
			.HasOne(r => r.User)
			.WithMany(u => u.Ratings)
			.HasForeignKey(r => r.UserId);

		modelBuilder.Entity<Rating>()
			.HasOne(r => r.Movie)
			.WithMany(m => m.Ratings)
			.HasForeignKey(r => r.MovieId);

		// MovieTape: зв’язок Movie -> MovieTape
		modelBuilder.Entity<MovieTape>()
			.HasOne(mt => mt.Movie)
			.WithMany(m => m.Tapes)
			.HasForeignKey(mt => mt.MovieId);
	}
}