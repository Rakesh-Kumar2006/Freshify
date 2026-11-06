using Freshify.Data;
using Freshify.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC services
builder.Services.AddControllersWithViews();

// Database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Cookie authentication
builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme
    )
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Create the first Admin if no Admin exists.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context =
        services.GetRequiredService<ApplicationDbContext>();

    var configuration =
        services.GetRequiredService<IConfiguration>();

    var adminExists = await context.Users
        .AnyAsync(u => u.Role == "Admin");

    if (!adminExists)
    {
        var fullName = configuration["InitialAdmin:FullName"];
        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Initial Admin settings are missing from configuration."
            );
        }

        var admin = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            Role = "Admin"
        };

        var hasher = new PasswordHasher<User>();

        admin.PasswordHash = hasher.HashPassword(
            admin,
            password
        );

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve CSS, JavaScript, and images from wwwroot.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Open the customer home page by default.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();