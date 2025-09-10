namespace API.IntegrationTests.Infrastructure;

// ITestDatabase abstracts how to get a connection string and how to clean the DB per test run.
public interface ITestDatabase
{
    string GetConnectionString();
    Task CleanAsync();
}