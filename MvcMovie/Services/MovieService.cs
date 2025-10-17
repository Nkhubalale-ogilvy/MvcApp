using MvcMovie.Data;
using MvcMovie.Interfaces;
using MvcMovie.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MvcMovie.Services
{
    public class MovieService : IMovieService
    {
        private readonly MvcMovieContext _context;
        private readonly ILogger<MovieService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MovieService(MvcMovieContext context, ILogger<MovieService> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IEnumerable<Movie>> GetAllMoviesAsync()
        {
            _logger.LogInformation("Retrieving all movies from database.");
            return await _context.Movie.ToListAsync();
        }

        public async Task<MovieGenreViewModel> GetFilteredMoviesAsync(string movieGenre, string searchString, string movieRating, bool isAjax)
        {
            _logger.LogInformation("Filtering movies. Genre: {Genre}, Search: {Search}, Rating: {Rating}", 
                                movieGenre, searchString, movieRating);

            if (_context.Movie == null)
            {
                _logger.LogWarning("Movie DbSet is null in MvcMovieContext.");
                return new MovieGenreViewModel { Movies = new List<Movie>() };
            }

            IQueryable<string> genreQuery = from m in _context.Movie
                                            orderby m.Genre
                                            select m.Genre;

            IQueryable<string> ratingQuery = from m in _context.Movie
                                             orderby m.Rating
                                             select m.Rating;

            var movies = from m in _context.Movie
                         select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title!.ToUpper().Contains(searchString.ToUpper()));
            }

            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            if (!string.IsNullOrEmpty(movieRating))
            {
                movies = movies.Where(x => x.Rating == movieRating);
            }

            var movieGenreVM = new MovieGenreViewModel
            {
                Genres = new SelectList(await genreQuery.Distinct().ToListAsync()),
                Ratings = new SelectList(await ratingQuery.Distinct().ToListAsync()),
                Movies = await movies.ToListAsync(),
                MovieGenre = movieGenre,
                MovieRating = movieRating,
                SearchString = searchString
            };

            return movieGenreVM;
        }

        public async Task<Movie?> GetMovieByIdAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("GetMovieByIdAsync called with null ID.");
                return null;
            }

            _logger.LogInformation("Retrieving movie with ID: {MovieId}", id);
            return await _context.Movie.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> CreateMovieAsync(Movie movie, IFormFile? image, IFormFile? video)
        {
            try
            {
                if (image != null && image.Length > 0)
                {
                    movie.ImagePath = await SaveFileAsync(image, "movie-images");
                }

                if (video != null && video.Length > 0)
                {
                    movie.VideoPath = await SaveFileAsync(video, "movie-videos");
                }

                _context.Add(movie);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully created movie: {MovieTitle} (ID: {MovieId})", movie.Title, movie.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create movie: {MovieTitle}", movie.Title);
                return false;
            }
        }

        public async Task<bool> UpdateMovieAsync(Movie movie, IFormFile? image, IFormFile? video)
        {
            try
            {
                var existingMovie = await _context.Movie
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == movie.Id);
                    
                if (existingMovie == null)
                {
                    _logger.LogWarning("Movie with ID {MovieId} not found for update.", movie.Id);
                    return false;
                }

                if (image != null && image.Length > 0)
                {
                    movie.ImagePath = await SaveFileAsync(image, "movie-images");
                }
                else
                {
                    movie.ImagePath = existingMovie.ImagePath;
                }

                if (video != null && video.Length > 0)
                {
                    movie.VideoPath = await SaveFileAsync(video, "movie-videos");
                }
                else
                {
                    movie.VideoPath = existingMovie.VideoPath;
                }

                _context.Update(movie);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully updated movie: {MovieTitle} (ID: {MovieId})", movie.Title, movie.Id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency exception during movie update for ID {MovieId}", movie.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update movie: {MovieTitle} (ID: {MovieId})", movie.Title, movie.Id);
                return false;
            }
        }

        public async Task<bool> DeleteMovieAsync(int id)
        {
            try
            {
                var movie = await _context.Movie.FindAsync(id);
                if (movie != null)
                {
                    if (!string.IsNullOrEmpty(movie.ImagePath))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, movie.ImagePath.TrimStart('/'));
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                            _logger.LogInformation("Deleted movie image file: {ImagePath}", imagePath);
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(movie.VideoPath))
                    {
                        var videoPath = Path.Combine(_webHostEnvironment.WebRootPath, movie.VideoPath.TrimStart('/'));
                        if (File.Exists(videoPath))
                        {
                            File.Delete(videoPath);
                            _logger.LogInformation("Deleted movie video file: {VideoPath}", videoPath);
                        }
                    }

                    _context.Movie.Remove(movie);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted movie ID: {MovieId}", id);
                    return true;
                }
                
                _logger.LogWarning("Attempted to delete movie ID {MovieId}, but movie was not found.", id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete movie ID: {MovieId}", id);
                return false;
            }
        }

        public async Task<bool> MovieExistsAsync(int id)
        {
            return await _context.Movie.AnyAsync(e => e.Id == id);
        }

        private async Task<string?> SaveFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}