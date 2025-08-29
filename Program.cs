using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EduMation.Data;
using EduMation.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Kestrel to allow larger file uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100L * 1024L * 1024L; // 100 MB
});

// Configure form options to handle large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100L * 1024L * 1024L; // 100 MB
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Enable detailed error pages in development
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        // Ensure database is created and migrations are applied
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw; // Rethrow to surface the error when running
    }

    try
    {
        // Seed roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            logger.LogInformation("Admin role created.");
        }

        // Seed admin user
        var adminEmail = "admin@edumation.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user created and assigned to Admin role.");

                // Seed admin profile if not exists
                var adminProfile = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == adminUser.Id);
                if (adminProfile == null)
                {
                    adminProfile = new Profile
                    {
                        UserId = adminUser.Id,
                        FirstName = "Admin",
                        LastName = "User",
                    };
                    context.Profiles.Add(adminProfile);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Admin profile seeded.");
                }
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Seed sample videos
        if (!context.Videos.Any())
        {
            context.Videos.AddRange(
                new Video
                {
                    Title = "Introduction to Programming",
                    Genre = "Education",
                    Description = "Learn the basics of programming.",
                    VideoUrl = "/Uploads/videos/sample-video.mp4",
                    ThumbnailUrl = "/Uploads/thumbnails/sample-thumbnail.jpg",
                    UploadDate = DateTime.Now
                },
                new Video
                {
                    Title = "Advanced Mathematics",
                    Genre = "Education",
                    Description = "Explore advanced mathematical concepts.",
                    VideoUrl = "/Uploads/videos/math-video.mp4",
                    ThumbnailUrl = "/Uploads/thumbnails/math-thumbnail.jpg",
                    UploadDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Sample videos seeded.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw; // Rethrow to surface the error when running
    }
}

app.Run();