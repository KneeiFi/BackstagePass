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
	public DbSet<Comment> Comments { get; set; }
	public DbSet<LikeComment> LikeComments { get; set; }
	public DbSet<Playlist> Playlists { get; set; }
	public DbSet<UserSubscription> UserSubscriptions { get; set; }
	public DbSet<WatchRoom> WatchRooms { get; set; } = default!;
	public DbSet<WatchRoomUser> WatchRoomUsers { get; set; } = default!;
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

		// Comment: зв’язок User -> Comment -> Movie
		modelBuilder.Entity<Comment>()
			.HasOne(c => c.User)
			.WithMany(u => u.Comments)
			.HasForeignKey(c => c.UserId);

		modelBuilder.Entity<Comment>()
			.HasOne(c => c.Movie)
			.WithMany(m => m.Comments)
			.HasForeignKey(c => c.MovieId);

		// LikeComment: зв’язок Comment <-> User
		modelBuilder.Entity<LikeComment>()
			.HasOne(lc => lc.User)
			.WithMany(u => u.LikeComments)
			.HasForeignKey(lc => lc.UserId);

		modelBuilder.Entity<LikeComment>()
			.HasOne(lc => lc.Comment)
			.WithMany(c => c.LikeComments)
			.HasForeignKey(lc => lc.CommentId);

		// Playlist: зв’язок із User та Movie
		modelBuilder.Entity<Playlist>()
			.HasOne(p => p.User)
			.WithMany(u => u.Playlists)
			.HasForeignKey(p => p.UserId);

		modelBuilder.Entity<Playlist>()
			.HasOne(p => p.Movie)
			.WithMany(m => m.Playlists)
			.HasForeignKey(p => p.MovieId);

		// Composite index on Playlist: UserId, Title, MovieId
		modelBuilder.Entity<Playlist>()
		.HasIndex(p => new { p.UserId, p.Title, p.MovieId });

		// I didn't even know about this but now i can do shitty things like this playlist logic and still this will be fast anough probably :)
		modelBuilder.Entity<Playlist>()
	    .HasIndex(p => new { p.UserId, p.Title });

		// UserSubscription: дві навігації на User
		modelBuilder.Entity<UserSubscription>()
			.HasOne(us => us.User1)
			.WithMany(u => u.Subscriptions)
			.HasForeignKey(us => us.User1Id)
			.OnDelete(DeleteBehavior.Restrict); // щоб уникнути каскадних конфліктів

		modelBuilder.Entity<UserSubscription>()
			.HasOne(us => us.User2)
			.WithMany(u => u.Subscribers)
			.HasForeignKey(us => us.User2Id)
			.OnDelete(DeleteBehavior.Restrict);

		// WatchRoom: 
		modelBuilder.Entity<WatchRoom>()
		.HasIndex(r => r.RoomCode)
		.IsUnique(); // Чтобы комната имела уникальный код

		modelBuilder.Entity<WatchRoomUser>()
			.HasOne(u => u.WatchRoom)
			.WithMany(r => r.Users)
			.HasForeignKey(u => u.WatchRoomId)
			.OnDelete(DeleteBehavior.Cascade); // Если комната удалена — удалить и участников

	}
}