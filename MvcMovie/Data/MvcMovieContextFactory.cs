using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MvcMovie.Data
{
 public class MvcMovieContextFactory : IDesignTimeDbContextFactory<MvcMovieContext>
 {
     public MvcMovieContext CreateDbContext(string[] args)
     {
         // Build configuration
         IConfigurationRoot configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json")
             .Build();

         // Use SQLite (or SQL Server depending on your setup)
         var optionsBuilder = new DbContextOptionsBuilder<MvcMovieContext>();
         optionsBuilder.UseSqlite(configuration.GetConnectionString("MvcMovieContext"));

         return new MvcMovieContext(optionsBuilder.Options);
     }
 }
}