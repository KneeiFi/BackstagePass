namespace BackStagePassServer.Models;

public class EmailConfirm
{
	public int Id { get; set; }

	public string UserEmail { get; set; } // Email, по которому регистрируем

	public string Key { get; set; }

	public DateTime ExpiryDate { get; set; }
}
