using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace API.IntegrationTests.Infrastructure;

// A concrete SQL Server test database using Testcontainers.
// Implements xUnit's IAsyncLifetime so the container starts once per collection fixture.
public class MsSqlTestDatabase : IAsyncLifetime, ITestDatabase
{
    // Build a disposable SQL Server container.
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();

    // Respawn instance lazily created on first clean.
    private Respawner? _respawner;

    // Cached connection string (with our desired DB name).
    private string _connectionString = default!;

    // Start the container when the test collection begins.
    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync().ConfigureAwait(false);

        // Compose a connection string with our own catalog and trust settings for local TLS.
        var connectionStringBuilder = new SqlConnectionStringBuilder(_msSqlContainer.GetConnectionString())
        {
            InitialCatalog = "testDb",
            TrustServerCertificate = true
        };
        _connectionString = connectionStringBuilder.ToString();
    }

    // Dispose the container when the collection finishes.
    public Task DisposeAsync() =>
        _msSqlContainer.DisposeAsync().AsTask();

    // Reset database state between tests using Respawn.
    public async Task CleanAsync()
    {
        // Create the respawner once and reuse it (fast).
        _respawner ??= await Respawner.CreateAsync(_connectionString, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,                            // Target engine
            TablesToIgnore = [new Table("__EFMigrationsHistory")],      // Keep EF migration metadata
            WithReseed = true                                           // Reset IDENTITY values
        }).ConfigureAwait(false);

        await _respawner.ResetAsync(_connectionString);
    }

    // Expose the connection string for the WebApplicationFactory override.
    public string GetConnectionString() => _connectionString;
}