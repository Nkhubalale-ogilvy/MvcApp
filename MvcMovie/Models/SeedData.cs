using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Data;
using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace MvcMovie.Models;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        // Get the DbContext, RoleManager, and UserManager from the service provider
        var context = serviceProvider.GetRequiredService<MvcMovieContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the database is created and migrations are applied
        context.Database.Migrate();

        // Seed roles
        string[] roleNames = { "Admin", "User" };
        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Seed admin user
        var adminUser = await userManager.FindByEmailAsync("Nkadimeng@example.com");
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "Nkadimeng@example.com",
                Email = "Nkadimeng@example.com",
                EmailConfirmed = true
            };
            
            var createAdmin = await userManager.CreateAsync(adminUser, "Ogilvy123!");
            if (createAdmin.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                // Handle password requirements if needed
                var errors = string.Join(", ", createAdmin.Errors.Select(e => e.Description));
                // Log or handle the errors appropriately
            }
        }
        // Look for any movies.
        if (context.Movie.Any())
        {
            return;   // DB has been seeded
        }

        context.Movie.AddRange(
            new Movie
            {
                Title = "When Harry Met Sally",
                ReleaseDate = DateTime.Parse("1989-2-12"),
                Genre = "Romantic Comedy",
                Rating = "R",
                Price = 7.99M
            },
            new Movie
            {
                Title = "Ghostbusters ",
                ReleaseDate = DateTime.Parse("1984-3-13"),
                Genre = "Comedy",
                Rating = "PG",
                Price = 8.99M
            },
            new Movie
            {
                Title = "Ghostbusters 2",
                ReleaseDate = DateTime.Parse("1986-2-23"),
                Genre = "Comedy",
                Rating = "PG",
                Price = 9.99M
            },
            new Movie
            {
                Title = "Rio Bravo",
                ReleaseDate = DateTime.Parse("1959-4-15"),
                Genre = "Western",
                Rating = "G",
                Price = 3.99M
            }
        );
        await context.SaveChangesAsync();
    }
}