namespace API.IntegrationTests.Infrastructure;

// xUnit collection to share a single MsSqlTestDatabase per test run (faster).
[CollectionDefinition("MsSqlDatabaseCollection")]
public class MsSqlDatabaseCollectionDefinition : ICollectionFixture<MsSqlTestDatabase> { }