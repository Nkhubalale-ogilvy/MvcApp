using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MvcMovie.Data;
using MvcMovie.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        formatter: new CompactJsonFormatter(),
        path: "logs/log.json",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 1 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1)
    )
    .CreateLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // Integrate Serilog
    builder.Host.UseSerilog();

    // DbContext configuration
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddDbContext<MvcMovieContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("MvcMovieContext") ?? throw new InvalidOperationException("Connection string 'MvcMovieContext' not found.")));
    }
    else
    {
        builder.Services.AddDbContext<MvcMovieContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ProductionMvcMovieContext") ?? throw new InvalidOperationException("Connection string 'ProductionMvcMovieContext' not found.")));
    }

    // Identity configuration
    builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<MvcMovieContext>();

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    // Seed data initialization
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<MvcMovieContext>();
        context.Database.Migrate();

        await SeedData.Initialize(services);
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}