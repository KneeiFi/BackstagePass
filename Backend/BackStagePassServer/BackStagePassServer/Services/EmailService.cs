using System.Net.Mail;
using System.Net;

namespace BackStagePassServer.Services;

public class EmailService : IEmailService
{
	private readonly IConfiguration _config;

	public EmailService(IConfiguration config)
	{
		_config = config;
	}

	public async Task SendConfirmationEmail(string toEmail, string confirmUrl)
	{
		var fromEmail = _config["Smtp:From"];
		var smtpHost = _config["Smtp:Host"];
		var smtpPort = int.Parse(_config["Smtp:Port"]);
		var username = _config["Smtp:Username"];
		var password = _config["Smtp:Password"];

		var message = new MailMessage(fromEmail, toEmail)
		{
			Subject = "Email Confirmation",
			Body = $"Please confirm your email: {confirmUrl}",
			//IsBodyHtml = false
		};

		using var client = new SmtpClient(smtpHost, smtpPort)
		{
			Credentials = new NetworkCredential(username, password),
			EnableSsl = true
		};

		await client.SendMailAsync(message);
	}
}
