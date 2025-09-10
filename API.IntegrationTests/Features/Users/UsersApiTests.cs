using API.IntegrationTests.Infrastructure;
using API.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Features.Users;

// Example API tests using Shouldly for readable assertions.
[Collection("MsSqlDatabaseCollection")]
public class UsersApiTests
{
    private readonly MsSqlTestDatabase _db;

    private static readonly Action<IServiceCollection> DefaultConfigure = services =>
    {
        // Put common test-specific service registrations here if needed.
    };

    public UsersApiTests(MsSqlTestDatabase db) => _db = db;

    [Fact]
    public async Task PostUser_Creates_AndReturns201_WithBody_And_LocationHeader()
    {
        // Arrange
        Action<IServiceCollection> additionalConfigure = services =>
        {
            // Per-test overrides, e.g., mock email sender
        };

        using var factory = ApiTestFactory.Create(_db, DefaultConfigure, additionalConfigure);
        var client = await factory.CreateApiClientAsync();
        var toCreate = new { Name = "Leia", Email = "leia@test.example" };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", toCreate);
        var created = await response.Content.ReadFromJsonAsync<User>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldStartWith("/api/users/");

        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("Leia");
        created.Email.ShouldBe("leia@test.example");
    }

    [Fact]
    public async Task GetUsers_Empty_ReturnsEmptyList()
    {
        // Arrange
        using var factory = ApiTestFactory.Create(_db);
        var client = await factory.CreateApiClientAsync();

        // Act
        var response = await client.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<User>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        users.ShouldNotBeNull();
        users.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUsers_ReturnsAllCreatedUsers()
    {
        // Arrange
        using var factory = ApiTestFactory.Create(_db);
        var client = await factory.CreateApiClientAsync();

        // Seed via DI scope to bypass API and speed up setup.
        await using (var scope = await factory.CreateAsyncScopeAsync())
        {
            var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbCtx.Users.AddRange(
                new User { Name = "Han", Email = "han@test.example" },
                new User { Name = "Chewie", Email = "chewie@test.example" }
            );
            await dbCtx.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<User>>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        users.ShouldNotBeNull();
        users.Count.ShouldBe(2);
        users.ShouldContain(u => u.Name == "Han");
        users.ShouldContain(u => u.Name == "Chewie");
    }

    [Fact]
    public async Task GetUser_NotFound_Returns404()
    {
        // Arrange
        using var factory = ApiTestFactory.Create(_db);
        var client = await factory.CreateApiClientAsync();

        // Act
        var response = await client.GetAsync("/api/users/999999");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUser_ById_ReturnsUser_AfterCreation()
    {
        // Arrange
        using var factory = ApiTestFactory.Create(_db);
        var client = await factory.CreateApiClientAsync();

        var user = new User { Name = "Luke", Email = "luke@test.example" };
        await using (var scope = await factory.CreateAsyncScopeAsync())
        {
            var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbCtx.Users.Add(user);
            await dbCtx.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync($"/api/users/{user.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<User>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(user.Id);
        fetched.Name.ShouldBe("Luke");
        fetched.Email.ShouldBe("luke@test.example");
    }
}