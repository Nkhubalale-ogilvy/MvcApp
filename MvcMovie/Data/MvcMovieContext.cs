using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Models;

namespace MvcMovie.Data
{
    public class MvcMovieContext : IdentityDbContext<IdentityUser>
    {
        public MvcMovieContext(DbContextOptions<MvcMovieContext> options) 
            : base(options)
        {
        }

        public DbSet<Movie> Movie { get; set; } = default!;
    }
}