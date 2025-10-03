using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MvcMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MvcMovieContext _context;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(MvcMovieContext context, ILogger<MoviesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Movies
        public async Task<IActionResult> Index(string movieGenre, string searchString, string movieRating, bool isAjax = false)
        {
            _logger.LogInformation("User accessed Movies Index page. Genre: {Genre}, Search: {Search}, Rating: {Rating}, IsAjax: {IsAjax}",
                                movieGenre, searchString, movieRating, isAjax);

            if (_context.Movie == null)
            {
                _logger.LogWarning("Movie DbSet is null in MvcMovieContext. Cannot retrieve movies.");
                return Problem("Entity set 'MvcMovieContext.Movie' is null.");
            }

            // Use LINQ to get list of genres.
            IQueryable<string> genreQuery = from m in _context.Movie
                                            orderby m.Genre
                                            select m.Genre;

            // Use LINQ to get list of ratings.
            IQueryable<string> ratingQuery = from m in _context.Movie
                                             orderby m.Rating
                                             select m.Rating;

            var movies = from m in _context.Movie
                         select m;

            // Apply search string filter
            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title!.ToUpper().Contains(searchString.ToUpper()));
            }

            // Apply genre filter
            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            // Apply rating filter
            if (!string.IsNullOrEmpty(movieRating))
            {
                movies = movies.Where(x => x.Rating == movieRating);
            }

            // Create the ViewModel, including the new Ratings SelectList
            var movieGenreVM = new MovieGenreViewModel
            {
                Genres = new SelectList(await genreQuery.Distinct().ToListAsync()),
                Ratings = new SelectList(await ratingQuery.Distinct().ToListAsync()),
                Movies = await movies.ToListAsync(),
                MovieGenre = movieGenre,
                MovieRating = movieRating,
                SearchString = searchString
            };

            // Return PartialView for AJAX requests
            if (isAjax)
            {
                return PartialView("_MovieListPartial", movieGenreVM);
            }

            return View(movieGenreVM);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("User requested Movie Details for ID: {MovieId}", id);

            if (id == null)
            {
                _logger.LogWarning("Movie Details requested with null ID.");
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found for details.", id);
                return NotFound();
            }

            return View(movie);
        }

        // GET: Movies/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            _logger.LogInformation("Admin user {UserName} accessed Create Movie form.", User.Identity?.Name ?? "Unknown");
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user {UserName} created new movie: {MovieTitle} (ID: {MovieId})", User.Identity?.Name ?? "Unknown", movie.Title, movie.Id);
                return RedirectToAction(nameof(Index));
            }
            _logger.LogWarning("Admin user {UserName} failed to create movie. Model state invalid for movie: {@Movie}", User.Identity?.Name ?? "Unknown", movie);
            return View(movie);
        }

        // GET: Movies/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Admin user {UserName} accessed Edit Movie form for ID: {MovieId}", User.Identity?.Name ?? "Unknown", id);

            if (id == null)
            {
                _logger.LogWarning("Edit Movie requested with null ID.");
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found for editing.", id);
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie)
        {
            _logger.LogInformation("Admin user {UserName} attempting to save edits for movie ID: {MovieId}", User.Identity?.Name ?? "Unknown", id);

            if (id != movie.Id)
            {
                _logger.LogError("Movie ID mismatch during edit. Route ID: {RouteId}, Movie Object ID: {MovieObjectId}", id, movie.Id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Admin user {UserName} successfully updated movie: {MovieTitle} (ID: {MovieId})", User.Identity?.Name ?? "Unknown", movie.Title, movie.Id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!MovieExists(movie.Id))
                    {
                        _logger.LogWarning("Concurrency exception during movie update for ID {MovieId}: Movie no longer exists.", movie.Id);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency exception during movie update for ID {MovieId}.", movie.Id);
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            _logger.LogWarning("Admin user {UserName} failed to edit movie. Model state invalid for movie: {@Movie}", User.Identity?.Name ?? "Unknown", movie);
            return View(movie);
        }

        // GET: Movies/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Admin user {UserName} accessed Delete Movie confirmation for ID: {MovieId}", User.Identity?.Name ?? "Unknown", id);

            if (id == null)
            {
                _logger.LogWarning("Delete Movie requested with null ID.");
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                _logger.LogWarning("Movie with ID {MovieId} not found for deletion confirmation.", id);
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Admin user {UserName} confirmed deletion of movie ID: {MovieId}", User.Identity?.Name ?? "Unknown", id);

            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin user {UserName} successfully deleted movie ID: {MovieId}", User.Identity?.Name ?? "Unknown", id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete movie ID {MovieId}, but movie was not found.", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }
}