using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Infrastructure;

// Factory contract for tests to create DI scopes and HTTP clients.
public interface IApiTestFactory : IDisposable
{
    Task<AsyncServiceScope> CreateAsyncScopeAsync();
    Task<HttpClient> CreateApiClientAsync(WebApplicationFactoryClientOptions? options = null);
}