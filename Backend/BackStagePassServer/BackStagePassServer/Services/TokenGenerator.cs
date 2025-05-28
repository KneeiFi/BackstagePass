using System.Security.Cryptography;

namespace BackStagePassServer.Services;

public class TokenGenerator
{
	public static string GenerateToken(int size = 64)
	{
		var rng = RandomNumberGenerator.Create();
		var bytes = new byte[size];
		rng.GetBytes(bytes);

		// Base64UrlSafe
		return Convert.ToBase64String(bytes)
			.Replace("+", "2")
			.Replace("/", "1")
			.Replace("=", "9"); // padding
	}
}
