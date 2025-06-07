namespace BackStagePassServer.Models;

public class RessetPassword
{
	public int Id { get; set; }

	public string UserEmail { get; set; } // Email, по которому регистрируем

	public string Password { get; set; }

	public string Key { get; set; }

	public DateTime ExpiryDate { get; set; }
}