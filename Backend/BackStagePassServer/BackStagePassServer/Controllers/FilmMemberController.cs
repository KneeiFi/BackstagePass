using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilmMemberController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _context;
	private readonly IPosterService _posterService;

	public FilmMemberController(AppDbContext context, AuthService auth, IPosterService posterService)
	{
		_context = context;
		_authService = auth;
		_posterService = posterService;
	}

	// Gets a FilmMember by Id
	[HttpGet("{id}")]
	public async Task<IActionResult> GetFilmMemberById(int id)
	{
		var filmMember = await _context.FilmMembers
		.FirstOrDefaultAsync(fm => fm.Id == id);

		if (filmMember == null)
			return NoContent();

		var response = new FilmMemberResponseDto
		{
			Id = filmMember.Id,
			FIO = filmMember.FIO,
			Role = filmMember.Role,
			PictureUrl = filmMember.PictureUrl != null
				? $"{Request.Scheme}://{Request.Host}/profiles/{filmMember.PictureUrl}"
				: null
		};

		return Ok(response);
	}

	// Gets a FilmMember by FIO
	[HttpGet("by-fio/{fio}")]
	public async Task<IActionResult> GetFilmMemberByFio(string fio)
	{
		var filmMember = await _context.FilmMembers
		.FirstOrDefaultAsync(fm => fm.FIO == fio);

		if (filmMember == null)
			return NoContent();

		var response = new FilmMemberResponseDto
		{
			Id = filmMember.Id,
			FIO = filmMember.FIO,
			Role = filmMember.Role,
			PictureUrl = filmMember.PictureUrl != null
		? $"{Request.Scheme}://{Request.Host}/profiles/{filmMember.PictureUrl}"
		: null
		};

		return Ok(response);
	}

	// Gets all FilmMembers by MovieId
	[HttpGet("by-movie/{movieId}")]
	public async Task<IActionResult> GetFilmMembersByMovieId(int movieId)
	{
		var movie = await _context.Movies
			.Include(m => m.MovieFilmMembers)
			.ThenInclude(mfm => mfm.FilmMember)
			.FirstOrDefaultAsync(m => m.Id == movieId);

		if (movie == null)
			return NoContent();

		var filmMembers = movie.MovieFilmMembers
		.Select(mfm => new FilmMemberResponseDto
		{
			Id = mfm.FilmMember.Id,
			FIO = mfm.FilmMember.FIO,
			Role = mfm.FilmMember.Role,
			PictureUrl = mfm.FilmMember.PictureUrl != null
				? $"{Request.Scheme}://{Request.Host}/profiles/{mfm.FilmMember.PictureUrl}"
				: null
		})
		.ToList();

		return Ok(filmMembers);
	}

	// Adds a new FilmMember to the database
	[HttpPost("add")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> AddFilmMember([FromForm] FilmMemberDto filmMemberDto,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before uploading videos." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can add film members." } );
		
		if (filmMemberDto == null)
			return BadRequest();

		var filmMember = new FilmMember
		{
			FIO = filmMemberDto.FIO,
			Role = filmMemberDto.Role,
			PictureUrl = filmMemberDto.Picture != null ? await _posterService.SaveProfileAsync(filmMemberDto.Picture) : null
		};

		_context.FilmMembers.Add(filmMember);
		await _context.SaveChangesAsync();
		return Ok(new { Id = filmMember.Id });
	}

	// Adds a FilmMember to a Movie by MovieId, creates FilmMember if not exists
	[HttpPost("add-to-movie/{movieId}")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> AddFilmMemberToMovie(int movieId, [FromForm] FilmMemberDto filmMemberDto,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before uploading videos." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can add film members." });

		if (filmMemberDto == null)
			return BadRequest();

		// Check if the film member exists (by unique property, e.g., FIO + Role)
		var existingMember = await _context.FilmMembers
			.FirstOrDefaultAsync(fm => (fm.FIO == filmMemberDto.FIO) && (fm.Role == filmMemberDto.Role));

		FilmMember memberToAdd;
		if (existingMember == null)
		{
			memberToAdd = new FilmMember
			{
				FIO = filmMemberDto.FIO,
				Role = filmMemberDto.Role,
				PictureUrl = filmMemberDto.Picture != null ? await _posterService.SaveProfileAsync(filmMemberDto.Picture) : null
			};
			_context.FilmMembers.Add(memberToAdd);
			await _context.SaveChangesAsync();
		}
		else
		{
			if(existingMember.PictureUrl == null && filmMemberDto.Picture != null)
			{
				// Update existing member's picture if provided
				existingMember.PictureUrl = await _posterService.SaveProfileAsync(filmMemberDto.Picture);
				_context.FilmMembers.Update(existingMember);
				await _context.SaveChangesAsync();
			}
			memberToAdd = existingMember;
		}

		// Check if the Movie exists
		var movie = await _context.Movies
			.Include(m => m.MovieFilmMembers)
			.FirstOrDefaultAsync(m => m.Id == movieId);

		if (movie == null)
			return NotFound("Movie not found.");

		// Add the FilmMember to the Movie if not already added
		if (!movie.MovieFilmMembers.Any(fm => (fm.FilmMemberId == memberToAdd.Id) && (fm.MovieId == movieId)))
		{
			var movieFilmMember = new MovieFilmMember
			{
				MovieId = movie.Id,
				FilmMemberId = memberToAdd.Id
			};
			_context.MovieFilmMembers.Add(movieFilmMember);
			await _context.SaveChangesAsync();
		}

		return Ok(new { Id = memberToAdd.Id });
	}

	// Deletes a FilmMember by Id and removes from all MovieFilmMembers
	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteFilmMemberById(int id,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before deleting film members." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can delete film members." });

		var filmMember = await _context.FilmMembers
			.Include(fm => fm.MovieFilmMembers)
			.FirstOrDefaultAsync(fm => fm.Id == id);

		if (filmMember == null)
			return NotFound("Film member not found.");

		// Remove all MovieFilmMembers associations
		if (filmMember.MovieFilmMembers != null && filmMember.MovieFilmMembers.Any())
		{
			_context.MovieFilmMembers.RemoveRange(filmMember.MovieFilmMembers);
		}

		if (filmMember.PictureUrl != null)
		{
			// Delete the picture file if it exists
			await _posterService.DeleteFileByNameAsync(filmMember.PictureUrl);
		}

		_context.FilmMembers.Remove(filmMember);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Film member deleted successfully." });
	}

	// Updates a FilmMember by Id
	[HttpPut("{id}")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UpdateFilmMemberById(int id, [FromForm] FilmMemberDto filmMemberDto, 
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token" });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating film members." });

		if (user.Role != UserRole.Admin)
			return BadRequest(new { error = "Only admins can update film members." });

		var filmMember = await _context.FilmMembers.FirstOrDefaultAsync(fm => fm.Id == id);
		if (filmMember == null)
			return NotFound("Film member not found.");

		if (filmMemberDto == null)
			return BadRequest();

		filmMember.FIO = filmMemberDto.FIO;
		filmMember.Role = filmMemberDto.Role;

		if (filmMemberDto.Picture != null)
		{
			if (filmMember.PictureUrl != null)
			{
				// Delete old picture if it exists
				await _posterService.DeleteFileByNameAsync(filmMember.PictureUrl);
			}
			filmMember.PictureUrl = await _posterService.SaveProfileAsync(filmMemberDto.Picture);
		}

		_context.FilmMembers.Update(filmMember);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Film member updated successfully." });
	}


}
