using BackStagePassServer.Models;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer;

public class AppDbContext : DbContext
{
	public DbSet<EmailConfirm> EmailConfirms { get; set; }
	public DbSet<User> Users { get; set; }
	public DbSet<UserToken> UserTokens { get; set; }

	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>()
			.HasIndex(u => u.Email)
			.IsUnique();

		modelBuilder.Entity<UserToken>()
			.HasOne(t => t.User)
			.WithMany(u => u.Tokens)
			.HasForeignKey(t => t.UserId);

	}
}