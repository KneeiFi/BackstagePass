namespace BackStagePassServer.Models;

public class EmailConfirm
{
	public int Id { get; set; }

	public string UserEmail { get; set; } // Email, по которому регистрируем

	public string Key { get; set; }

	public DateTime ExpiryDate { get; set; }

	public int IsConfirmed { get; set; } = 0;

	public string TempPasswordHash { get; set; }

	public string TempUsername { get; set; }
}
