﻿namespace BackStagePassServer.DTOs;

// DTO for returning rating info
public class RatingDto
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public int MovieId { get; set; }
	public int Value { get; set; }
}
