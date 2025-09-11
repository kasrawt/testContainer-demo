using API.Models;
using Microsoft.EntityFrameworkCore;

// Register DbContext using appsettings connection string for normal runs.
// Test factory replaces this with the container connection string.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider
        .GetRequiredService<AppDbContext>()
        .Database
        .EnsureCreated();           // Use Migrate() in real apps
}

// Read all users (AsNoTracking for faster read-only queries).
app.MapGet("/api/users", async (AppDbContext db) =>
    await db.Users.AsNoTracking().ToListAsync());

// Get single user by Id with a clean 404 if missing.
app.MapGet("/api/users/{id:int}", async (int id, AppDbContext db) =>
    await db.Users.FindAsync(id) is { } user
    ? Results.Ok(user)
    : Results.NotFound());

// Create a user and return 201 Created with Location header.
app.MapPost("/api/users", async (User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
});

app.Run();

// Partial Program class so WebApplicationFactory<TEntryPoint> can locate it.
public partial class Program { }

// EF Core DbContext with a single Users DbSet for demo.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}