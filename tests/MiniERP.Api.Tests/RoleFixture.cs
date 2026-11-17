using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MiniERP.Controllers;
using Npgsql;
using Xunit;

namespace MiniERP.Api.Tests;

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RoleFixture.ConnectionVariable)))
            Skip = "Set MINIERP_TEST_DATABASE_CONNECTION to the dedicated local mini_erp_tests database.";
    }
}

public sealed class IntegrationTheoryAttribute : TheoryAttribute
{
    public IntegrationTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RoleFixture.ConnectionVariable)))
            Skip = "Set MINIERP_TEST_DATABASE_CONNECTION to the dedicated local mini_erp_tests database.";
    }
}

public sealed class RoleFactory(string connection, string bootstrapToken) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Database:InitializeSchema"] = "true",
            ["Auth:BootstrapToken"] = bootstrapToken,
            ["Logging:LogLevel:Default"] = "Warning"
        }));
    }

    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
    });
}

public sealed class RoleFixture : IAsyncLifetime
{
    public const string ConnectionVariable = "MINIERP_TEST_DATABASE_CONNECTION";
    public string Connection { get; private set; } = string.Empty;
    public string BootstrapToken { get; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    public string Password { get; } = Guid.NewGuid().ToString("N") + " Aa7!";
    public RoleFactory Factory { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;
    public UserSummary Administrator { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Connection = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Connection)) return;
        var target = new NpgsqlConnectionStringBuilder(Connection);
        if (target.Database != "mini_erp_tests" || target.Host is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Integration tests reset only the dedicated local database mini_erp_tests.");
        await using (var db = new NpgsqlConnection(Connection))
        {
            await db.OpenAsync();
            await using var reset = new NpgsqlCommand("DROP SCHEMA IF EXISTS auth CASCADE; DROP TABLE IF EXISTS public.customers CASCADE;", db);
            await reset.ExecuteNonQueryAsync();
        }
        Factory = new RoleFactory(Connection, BootstrapToken);
        Admin = Factory.NewClient();
        using var setup = await Send(Admin, HttpMethod.Post, "/api/auth/setup", new
        {
            userName = "admin-tests", displayName = "Administrator test", password = Password, setupToken = BootstrapToken
        });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Administrator = (await setup.Content.ReadFromJsonAsync<UserSummary>())!;
    }

    public async Task<HttpClient> Login(string userName, string? password = null)
    {
        var client = Factory.NewClient();
        using var response = await Send(client, HttpMethod.Post, "/api/auth/login", new { userName, password = password ?? Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    public async Task<UserSummary> NewUser(string role)
    {
        using var response = await Send(Admin, HttpMethod.Post, "/api/users", new
        {
            userName = "test-" + Guid.NewGuid().ToString("N"), displayName = "Përdorues test", password = Password, role
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserSummary>())!;
    }

    public static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path,
        object? body = null, bool withCsrf = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (withCsrf)
        {
            var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
            request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        }
        return await client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        Admin?.Dispose();
        if (Factory is not null) await Factory.DisposeAsync();
    }

    private sealed record CsrfResponse(string Token);
}

[CollectionDefinition("Roles", DisableParallelization = true)]
public sealed class RoleCollection : ICollectionFixture<RoleFixture>;
