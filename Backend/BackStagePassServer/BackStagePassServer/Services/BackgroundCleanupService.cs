using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Services;

public class BackgroundCleanupService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly TimeSpan _interval = TimeSpan.FromHours(1); // можно менять период

	public BackgroundCleanupService(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using (var scope = _serviceProvider.CreateScope())
				{
					var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

					var now = DateTime.UtcNow;

					// Очистка EmailConfirm
					await db.EmailConfirms
						.Where(c => c.ExpiryDate < now || c.IsConfirmed == 1)
						.ExecuteDeleteAsync(stoppingToken);


					// Очистка UserTokens
					await db.UserTokens
						.Where(t => t.RefreshTokenExpiryTime < now)
						.ExecuteDeleteAsync(stoppingToken);


					await db.SaveChangesAsync(stoppingToken);
				}

				await Task.Delay(_interval, stoppingToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during cleanup: {ex.Message}");
			}
		}
	}
}