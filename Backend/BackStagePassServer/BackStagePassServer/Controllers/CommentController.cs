using BackStagePassServer.DTOs;
using BackStagePassServer.Models;
using BackStagePassServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackStagePassServer.Controllers;

[Route("api/[controller]")]
public class CommentController : ControllerBase
{
	private readonly AuthService _authService;
	private readonly AppDbContext _context;

	public CommentController(AppDbContext context, AuthService authService)
	{
		_context = context;
		_authService = authService;
	}


	[HttpPost("add")]
	public async Task<IActionResult> AddComment([FromBody] AddCommentDto dto,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before commenting." });

		if (user.IsBanned == 1)
			return BadRequest(new { error = "You are banned and cannot add comments." });

		if (dto == null)
			return BadRequest(new { error = "Request body is required." });

		if (string.IsNullOrWhiteSpace(dto.Content))
			return BadRequest(new { error = "Comment content is required." });

		if (dto.Content.Length > 1000)
			return BadRequest(new { error = "Comment content is too long (max 1000 characters)." });

		var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == dto.MovieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var comment = new Comment
		{
			UserId = user.Id,
			MovieId = dto.MovieId,
			Content = dto.Content
		};
		_context.Comments.Add(comment);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Comment added successfully." });
	}

	[HttpPut("update/{commentId:int}")]
	public async Task<IActionResult> UpdateComment([FromRoute] int commentId,[FromBody] UpdateCommentDto dto,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before updating comments." });

		if (user.IsBanned == 1)
			return BadRequest(new { error = "You are banned and cannot update comments." });

		if (dto == null)
			return BadRequest(new { error = "Request body is required." });

		if (string.IsNullOrWhiteSpace(dto.Content))
			return BadRequest(new { error = "Comment content is required." });

		if (dto.Content.Length > 1000)
			return BadRequest(new { error = "Comment content is too long (max 1000 characters)." });

		var comment = await _context.Set<Comment>().FirstOrDefaultAsync(c => c.Id == commentId);
		if (comment == null)
			return NotFound(new { error = "Comment not found." });

		if (comment.UserId != user.Id && user.Role != UserRole.Admin)
			return BadRequest(new { error = "Can't update others comments" });

		comment.Content = dto.Content;
		_context.Update(comment);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Comment updated successfully." });
	}

	

	[HttpGet("user/{userId:int}/movie/{movieId:int}/comments")]
	public async Task<IActionResult> GetCommentsByUserIdAndMovieId([FromRoute] int userId, [FromRoute] int movieId)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
		if (user == null)
			return NotFound(new { error = "User not found." });

		var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var comments = await (
		from c in _context.Set<Comment>().Include(c => c.User)
		where c.UserId == userId && c.MovieId == movieId
		join like in _context.Set<LikeComment>() on c.Id equals like.CommentId into likesGroup
		select new CommentResponseDto
		{
			Id = c.Id,
			UserId = c.UserId,
			Username = c.User.Username,
			Content = c.Content,
			Likes = likesGroup.Count(l => l.Type == 1),
			Dislikes = likesGroup.Count(l => l.Type == -1),
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{c.User.AvatarUrl}"
		}
		).ToListAsync();

		return Ok(comments);
	}

	[HttpGet("my/movie/{movieId:int}/comments")]
	public async Task<IActionResult> GetMyCommentsByMovieId([FromRoute] int movieId,
		[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var comments = await (
		from c in _context.Set<Comment>().Include(c => c.User)
		where c.UserId == user.Id && c.MovieId == movieId
		join like in _context.Set<LikeComment>() on c.Id equals like.CommentId into likesGroup
		select new CommentResponseDto
		{
			Id = c.Id,
			UserId = c.UserId,
			Username = c.User.Username,
			Content = c.Content,
			Likes = likesGroup.Count(l => l.Type == 1),
			Dislikes = likesGroup.Count(l => l.Type == -1),
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{c.User.AvatarUrl}"
		}
		).ToListAsync();

		return Ok(comments);
	}


	[HttpGet("movie/{movieId:int}/comments")]
	public async Task<IActionResult> GetCommentsByMovieId([FromRoute] int movieId)
	{
		var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId);
		if (movie == null)
			return NotFound(new { error = "Movie not found." });

		var comments = await (
		from c in _context.Set<Comment>().Include(c => c.User)
		where c.MovieId == movieId
		join like in _context.Set<LikeComment>() on c.Id equals like.CommentId into likesGroup
		select new CommentResponseDto
		{
			Id = c.Id,
			UserId = c.UserId,
			Username = c.User.Username,
			Content = c.Content,
			Likes = likesGroup.Count(l => l.Type == 1),
			Dislikes = likesGroup.Count(l => l.Type == -1),
			AvatarUrl = $"{Request.Scheme}://{Request.Host}/profiles/{c.User.AvatarUrl}"
		}
		).ToListAsync();

		return Ok(comments);
	}


	[HttpDelete("delete/{commentId:int}")]
	public async Task<IActionResult> DeleteComment([FromRoute] int commentId,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		var comment = await _context.Set<Comment>().FirstOrDefaultAsync(c => c.Id == commentId);
		if (comment == null)
			return NotFound(new { error = "Comment not found." });

		if (comment.UserId != user.Id && user.Role != UserRole.Admin)
			return BadRequest(new {error = "Can't delete others comments"});

		// Remove all LikeComments associated with the comment before deleting the comment itself
		var likeComments = await _context.LikeComments.Where(lc => lc.CommentId == comment.Id).ToListAsync();
		_context.LikeComments.RemoveRange(likeComments);
		_context.Remove(comment);
		await _context.SaveChangesAsync();

		return Ok(new { message = "Comment deleted successfully." });
	}

	[HttpPost("like")]
	public async Task<IActionResult> LikeComment([FromBody] LikeCommentDto dto,
	[FromHeader(Name = "Authorization")] string accessToken)
	{
		var user = await _authService.GetUserByAccessToken(accessToken);
		if (user == null)
			return Unauthorized(new { error = "Invalid access token." });

		if (user.Role == null)
			return BadRequest(new { error = "Email not confirmed. Please confirm your email before rating comments." });

		if (user.IsBanned == 1)
			return BadRequest(new { error = "You are banned and cannot like comments." });

		if (dto == null)
			return BadRequest(new { error = "Request body is required." });

		var comment = await _context.Set<Comment>().FirstOrDefaultAsync(c => c.Id == dto.CommentId);
		if (comment == null)
			return NotFound(new { error = "Comment not found." });

		// Assume LikeType: 1 = Like, -1 = Dislike, 0 = Remove
		var existingLike = await _context.Set<LikeComment>()
			.FirstOrDefaultAsync(l => l.CommentId == dto.CommentId && l.UserId == user.Id);

		if (dto.Type == 0)
		{
			if (existingLike != null)
			{
				_context.Remove(existingLike);
				await _context.SaveChangesAsync();
			}
			return Ok(new { message = "Like/dislike removed." });
		}

		if (existingLike == null)
		{
			var like = new LikeComment
			{
				UserId = user.Id,
				CommentId = dto.CommentId,
				Type = dto.Type
			};
			_context.Add(like);
		}
		else
		{
			existingLike.Type = dto.Type;
			_context.Update(existingLike);
		}

		await _context.SaveChangesAsync();
		return Ok(new { message = dto.Type == 1 ? "Comment liked." : "Comment disliked." });
	}


}
