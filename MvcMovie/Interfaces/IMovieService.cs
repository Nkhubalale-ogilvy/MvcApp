using MvcMovie.Models;
using Microsoft.AspNetCore.Http; // For IFormFile
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList (used in MovieGenreViewModel)
using System.Collections.Generic; // For IEnumerable
using System.Threading.Tasks; // For Task

namespace MvcMovie.Interfaces
{
public interface IMovieService
{
    // Read operations
    Task<IEnumerable<Movie>> GetAllMoviesAsync();
    Task<MovieGenreViewModel> GetFilteredMoviesAsync(string movieGenre, string searchString, string movieRating, bool isAjax);
    Task<Movie?> GetMovieByIdAsync(int? id);

    // Write operations
    Task<bool> CreateMovieAsync(Movie movie, IFormFile? image, IFormFile? video);
    Task<bool> UpdateMovieAsync(Movie movie, IFormFile? image, IFormFile? video);
    Task<bool> DeleteMovieAsync(int id);

    // Utility
    Task<bool> MovieExistsAsync(int id);
}
}