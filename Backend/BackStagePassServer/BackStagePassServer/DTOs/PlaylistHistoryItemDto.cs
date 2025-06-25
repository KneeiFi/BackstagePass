namespace BackStagePassServer.DTOs;

// DTO for returning history items
public class PlaylistHistoryItemDto
{
	public int PlaylistId { get; set; }
	public int MovieId { get; set; }
	public string MovieTitle { get; set; }
	public float MovieRating { get; set; }
	public string MoviePosterURL { get; set; }
	public DateTime AddedAt { get; set; }
}
