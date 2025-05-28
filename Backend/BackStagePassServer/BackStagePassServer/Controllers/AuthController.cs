using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _db;
	private readonly IEmailService _emailService;

	public AuthController(AuthService authService, AppDbContext db, IEmailService emailService)
	{
		_authService = authService;
		_db = db;
		_emailService = emailService;
	}

	[HttpPost("signup")]
	public async Task<IActionResult> Signup([FromBody] RegisterDto dto)
	{
		// Проверка, не зарегистрирован ли уже такой email
		if (await _db.Users.AnyAsync(u => u.Email == dto.Email) ||
			await _db.EmailConfirms.AnyAsync(c => c.UserEmail == dto.Email && c.IsConfirmed == 0))
		{
			return BadRequest("Email already used or waiting for confirmation");
		}

		var confirmKey = Guid.NewGuid().ToString();

		// Сохраняем только EmailConfirm
		var confirm = new EmailConfirm
		{
			UserEmail = dto.Email,
			Key = confirmKey,
			ExpiryDate = DateTime.UtcNow.AddHours(1),
			IsConfirmed = 0,
			TempPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
			TempUsername = dto.Username
		};

		_db.EmailConfirms.Add(confirm);
		await _db.SaveChangesAsync();

		var baseUrl = $"{Request.Scheme}://{Request.Host}";
		var confirmUrl = $"{baseUrl}/api/Auth/confirm?key={confirmKey}";

		try
		{
			await _emailService.SendConfirmationEmail(dto.Email, confirmUrl);
		}
		catch
		{
			// Если не удалось отправить email, удаляем запись
			_db.EmailConfirms.Remove(confirm);
			await _db.SaveChangesAsync();
			return BadRequest("Failed to send confirmation email. Please try again later.");
		}

		return Ok("Confirmation email sent.");
	}

	[HttpPost("signin")]
	public async Task<IActionResult> Signin([FromBody] LoginDto dto)
	{
		var token = await _authService.LoginAsync(dto);
		return Ok(token);
	}

	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh([FromBody] string refreshToken)
	{
		var token = await _authService.RefreshAsync(refreshToken);
		return Ok(token);
	}

	[HttpGet("confirm")]
	public async Task<IActionResult> ConfirmEmail([FromQuery] string key)
	{
		var confirm = await _db.EmailConfirms.FirstOrDefaultAsync(c => c.Key == key && c.IsConfirmed == 0);

		if (confirm == null || confirm.ExpiryDate < DateTime.UtcNow)
		{
			return BadRequest("Invalid or expired confirmation key");
		}

		// Создаём пользователя
		var user = new User
		{
			Email = confirm.UserEmail,
			Username = confirm.TempUsername,
			PasswordHash = confirm.TempPasswordHash,
			IsEmailConfirmed = 1,
			Role = UserRole.User
		};

		_db.Users.Add(user);
		confirm.IsConfirmed = 1;


		await _db.SaveChangesAsync();

		// Можно сразу генерировать access+refresh
		var tokenPair = await _authService.GenerateTokensAsync(user);

		// Возврат токенов, либо редирект с кодом
		return Ok(tokenPair);

		// или return Redirect("https://your-frontend/confirmed")
	}
}

