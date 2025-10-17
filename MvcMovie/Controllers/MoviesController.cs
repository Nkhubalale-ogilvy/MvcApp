using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MvcMovie.Interfaces;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ILogger<MoviesController> _logger;

        public MoviesController(IMovieService movieService, ILogger<MoviesController> logger)
        {
            _movieService = movieService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string movieGenre, string searchString, string movieRating, bool isAjax = false)
        {
            _logger.LogInformation("User accessed Movies Index. Genre: {Genre}, Search: {Search}, Rating: {Rating}", 
                                movieGenre, searchString, movieRating);

            var movieGenreVM = await _movieService.GetFilteredMoviesAsync(movieGenre, searchString, movieRating, isAjax);

            if (movieGenreVM.Movies == null)
            {
                return Problem("Entity set 'Movie' is null from service.");
            }

            if (isAjax)
            {
                return PartialView("_MovieListPartial", movieGenreVM);
            }

            return View(movieGenreVM);
        }

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _movieService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating,ImagePath,VideoPath")] Movie movie, IFormFile? Image, IFormFile? Video)
        {
            if (ModelState.IsValid)
            {
                var success = await _movieService.CreateMovieAsync(movie, Image, Video);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Failed to create movie.");
            }
            return View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _movieService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Price,Rating,ImagePath,VideoPath")] Movie movie, IFormFile? Image, IFormFile? Video)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _movieService.UpdateMovieAsync(movie, Image, Video);
                    if (success)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError(string.Empty, "Failed to update movie.");
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    if (!await _movieService.MovieExistsAsync(movie.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(movie);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _movieService.GetMovieByIdAsync(id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _movieService.DeleteMovieAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}