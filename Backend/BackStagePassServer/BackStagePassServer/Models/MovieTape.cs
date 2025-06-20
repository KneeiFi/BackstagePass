﻿namespace BackStagePassServer.Models;

public enum MediaType { Trailer, Movie, BestMomments, Other}


public class MovieTape
{
	public int Id { get; set; }

	public int MovieId { get; set; }
	public Movie Movie { get; set; }

	public string TapeTitle { get; set; }
	public MediaType MediaType { get; set; }
	public string VideoUrl { get; set; }
	public string ThumbnailUrl { get; set; }
}