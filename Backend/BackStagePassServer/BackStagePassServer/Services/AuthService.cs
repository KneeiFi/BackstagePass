using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BackStagePassServer.Services;

public class AuthService
{
	private readonly AppDbContext _db;

	public AuthService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
	{
		var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

		if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
			throw new Exception("Invalid credentials");

		if (user.Role == null)
			throw new Exception("Email not confirmed");

		return await GenerateTokensAsync(user);
	}

	public async Task<TokenResponseDto> RefreshAsync(string refreshToken)
	{
		var token = await _db.UserTokens
			.Include(t => t.User)
			.FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

		if (token == null || token.RefreshTokenExpiryTime < DateTime.UtcNow)
			throw new Exception("Invalid or expired refresh token");

		// Удаляем старый токен
		_db.UserTokens.Remove(token);
		await _db.SaveChangesAsync();

		return await GenerateTokensAsync(token.User);
	}

	public async Task<TokenResponseDto> GenerateTokensAsync(User user)
	{
		var accessToken = TokenGenerator.GenerateToken(32);
		var refreshToken = TokenGenerator.GenerateToken(64);

		var accessTokenExpires = DateTime.UtcNow.AddMinutes(15);
		var refreshTokenExpires = DateTime.UtcNow.AddDays(2);

		var token = new UserToken
		{
			UserId = user.Id,
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			AccessTokenExpiryTime = accessTokenExpires,
			RefreshTokenExpiryTime = refreshTokenExpires
		};

		_db.UserTokens.Add(token);
		await _db.SaveChangesAsync();

		return new TokenResponseDto
		{
			AccessToken = accessToken,
			RefreshToken = refreshToken,
		};
	}

	public async Task<User> GetUserByAccessToken(string accessToken)
	{
		var token = await _db.UserTokens
			.Include(t => t.User)
			.FirstOrDefaultAsync(t => t.AccessToken == accessToken);

		if (token == null || token.AccessTokenExpiryTime < DateTime.UtcNow)
			return null;

		return token.User;
	}
}

