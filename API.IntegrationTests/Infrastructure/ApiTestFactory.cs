using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.IntegrationTests.Infrastructure;

// WebApplicationFactory that wires the app to our containerized SQL Server.
// Also ensures schema exists and triggers Respawn cleaning at first use.
public class ApiTestFactory : WebApplicationFactory<Program>, IApiTestFactory
{
    private readonly string _connectionString;
    private readonly ITestDatabase _db;
    private bool _dbCleaned;
    private readonly Action<IServiceCollection>[] _configureServices;

    public static IApiTestFactory Create(ITestDatabase db, params Action<IServiceCollection>[] configureServices)
    {
        ArgumentNullException.ThrowIfNull(db);
        return new ApiTestFactory(db, configureServices);
    }

    private ApiTestFactory(ITestDatabase db, Action<IServiceCollection>[] configureServices)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _configureServices = configureServices;
        _connectionString = db.GetConnectionString();
    }

    // Create an async DI scope for seeding data directly via DbContext.
    public async Task<AsyncServiceScope> CreateAsyncScopeAsync()
    {
        var services = Services.CreateAsyncScope();
        await EnsureDbCleaned().ConfigureAwait(false);
        return services;
    }

    // Create an HttpClient pointing to the in-memory TestServer (real DB under the hood).
    public async Task<HttpClient> CreateApiClientAsync(WebApplicationFactoryClientOptions? options = null)
    {
        var client = CreateClient(options ?? new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await EnsureDbCleaned().ConfigureAwait(false);
        return client;
    }

    // Clean the DB exactly once per factory instance (before first use).
    private Task EnsureDbCleaned()
    {
        if (_dbCleaned)
            return Task.CompletedTask;

        _dbCleaned = true;
        return _db.CleanAsync();
    }

    // Override the host to use SQL Server from the container instead of test defaults.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        //builder.UseEnvironment(Environments.Development);         // Uncomment if you need Dev-only services

        builder.ConfigureTestServices(services =>
        {
            // Replace existing DbContextOptions with SQL Server options for our container DB.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(_connectionString));

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;
            db.EnsureCreated();             // Prefer Migrate() in real projects

            // Allow optional per-test service overrides (e.g., mock external services).
            foreach (var c in _configureServices)
                c?.Invoke(services);
        });
    }
}