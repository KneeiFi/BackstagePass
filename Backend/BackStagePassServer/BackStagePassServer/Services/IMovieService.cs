using BackStagePassServer.DTOs;
using BackStagePassServer.Models;

namespace BackStagePassServer.Services;

public interface IMovieService
{
	Task<int> CreateMovieAndGenresAsync(UploadMovieWithGenresDto dto, User user, HttpRequest request);

}
