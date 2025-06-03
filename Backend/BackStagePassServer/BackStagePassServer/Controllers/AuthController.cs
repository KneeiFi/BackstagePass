using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
		// Проверка формата email
		if (string.IsNullOrWhiteSpace(dto.Email) || !new EmailAddressAttribute().IsValid(dto.Email))
		{
			return BadRequest(new { error = "Invalid email address format" });
		}

		// Проверка, не зарегистрирован ли уже такой email
		if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
		{
			return BadRequest(new { error = "Email already used or waiting for confirmation" });
		}

		// Хешируем пароль
		var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

		// Создаём нового пользователя (без Role)
		var user = new User
		{
			Email = dto.Email,
			Username = dto.Username,
			PasswordHash = passwordHash,
			AvatarUrl = "default", // Можно заменить на реальный URL аватара
			IsBanned = 0, // Не забанен
			Role = null // Подтвердит — станет User
		};

		_db.Users.Add(user);

		_db.SaveChanges();

		var confirmKey = Guid.NewGuid().ToString();

		// Сохраняем EmailConfirm
		var confirm = new EmailConfirm
		{
			UserEmail = dto.Email,
			Key = confirmKey,
			ExpiryDate = DateTime.UtcNow.AddHours(1),
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
			_db.Users.Remove(user);
			await _db.SaveChangesAsync();
			return BadRequest(new { error = "Failed to send confirmation email. Please try again later." });
		}

		return Ok(new { message = "Confirmation email sent." });
	}

	[HttpPost("check-email")]
	public async Task<IActionResult> CheckEmail([FromBody] string email)
	{
		if (email==null)
			return BadRequest(new { error = "Email is required" });

		var exists = await _db.Users.AnyAsync(u => u.Email == email && u.Role != null);

		return Ok(new { registered = exists });
	}

	[HttpPost("signin")]
	public async Task<IActionResult> Signin([FromBody] LoginDto dto)
	{
		try
		{
			var token = await _authService.LoginAsync(dto);
			return Ok(token);
		}
		catch (Exception ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh([FromBody] string refreshToken)
	{
		try
		{
			var token = await _authService.RefreshAsync(refreshToken);
			return Ok(token);
		}
		catch (Exception ex)
		{
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpGet("confirm")]
	public async Task<IActionResult> ConfirmEmail([FromQuery] string key)
	{
		
		var confirm = await _db.EmailConfirms.FirstOrDefaultAsync(c => c.Key == key);

		if (confirm == null || confirm.ExpiryDate < DateTime.UtcNow)
		{
			return BadRequest(new { error = "Invalid or expired confirmation key" });
		}

		var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == confirm.UserEmail);

		if (user == null)
		{
			return BadRequest(new { error = "User not found" });
		}

		user.Role = UserRole.User;

		// Можно также удалить запись подтверждения, если она больше не нужна
		_db.EmailConfirms.Remove(confirm);

		await _db.SaveChangesAsync();

		return Ok();
	}
}

