using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MiniERP.Controllers;
using MiniERP.Models;
using MiniERP.Security;
using Xunit;
using static MiniERP.Api.Tests.RoleFixture;

namespace MiniERP.Api.Tests;

[Collection("Roles")]
public sealed class RoleTests(RoleFixture fixture)
{
    [IntegrationFact]
    public async Task Anonymous_users_cannot_read_or_modify_customers()
    {
        using var client = fixture.Factory.NewClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/customers")).StatusCode);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete })
        {
            using var response = await Send(client, method, method == HttpMethod.Post ? "/api/customers" : "/api/customers/1", new { name = "Not allowed" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [IntegrationFact]
    public async Task Initial_setup_needs_the_private_token_and_cannot_be_repeated()
    {
        using var client = fixture.Factory.NewClient();
        using var wrong = await Send(client, HttpMethod.Post, "/api/auth/setup", new
        {
            userName = "other-admin", displayName = "Not allowed", password = fixture.Password, setupToken = "not-the-private-token"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using var repeated = await Send(client, HttpMethod.Post, "/api/auth/setup", new
        {
            userName = "other-admin", displayName = "Not allowed", password = fixture.Password, setupToken = fixture.BootstrapToken
        });
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [IntegrationTheory]
    [InlineData(AppRoles.Administrator, true, true)]
    [InlineData(AppRoles.Salesperson, true, true)]
    [InlineData(AppRoles.Accountant, true, false)]
    [InlineData(AppRoles.Warehouse, false, false)]
    public async Task Customer_permissions_are_enforced_by_the_API(string role, bool read, bool write)
    {
        var user = await fixture.NewUser(role);
        using var client = await fixture.Login(user.UserName);
        using var list = await client.GetAsync("/api/customers");
        Assert.Equal(read ? HttpStatusCode.OK : HttpStatusCode.Forbidden, list.StatusCode);
        using var existing = await Send(fixture.Admin, HttpMethod.Post, "/api/customers", new { name = "Role permissions test" });
        var customer = (await existing.Content.ReadFromJsonAsync<Customer>())!;
        try
        {
            using var one = await client.GetAsync($"/api/customers/{customer.Id}");
            Assert.Equal(read ? HttpStatusCode.OK : HttpStatusCode.Forbidden, one.StatusCode);
            using var added = await Send(client, HttpMethod.Post, "/api/customers", new { name = "Created by role" });
            Assert.Equal(write ? HttpStatusCode.Created : HttpStatusCode.Forbidden, added.StatusCode);
            if (write)
            {
                var own = (await added.Content.ReadFromJsonAsync<Customer>())!;
                using var removed = await Send(client, HttpMethod.Delete, $"/api/customers/{own.Id}");
                Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            }
            using var changed = await Send(client, HttpMethod.Put, $"/api/customers/{customer.Id}", new { name = "Updated by role" });
            Assert.Equal(write ? HttpStatusCode.OK : HttpStatusCode.Forbidden, changed.StatusCode);
            using var deleted = await Send(client, HttpMethod.Delete, $"/api/customers/{customer.Id}");
            Assert.Equal(write ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden, deleted.StatusCode);
            if (!write)
            {
                var unchanged = await fixture.Admin.GetFromJsonAsync<Customer>($"/api/customers/{customer.Id}");
                Assert.Equal("Role permissions test", unchanged!.Name);
            }
        }
        finally { using var cleanup = await Send(fixture.Admin, HttpMethod.Delete, $"/api/customers/{customer.Id}"); }
    }

    [IntegrationTheory]
    [InlineData(AppRoles.Salesperson)]
    [InlineData(AppRoles.Accountant)]
    [InlineData(AppRoles.Warehouse)]
    public async Task Only_administrators_can_manage_users(string role)
    {
        var account = await fixture.NewUser(role);
        using var client = await fixture.Login(account.UserName);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
        using var create = await Send(client, HttpMethod.Post, "/api/users", new
        {
            userName = "intruder", displayName = "Not allowed", password = fixture.Password, role = AppRoles.Administrator
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        using var escalate = await Send(client, HttpMethod.Put, $"/api/users/{account.Id}", new
        {
            displayName = account.DisplayName, role = AppRoles.Administrator, isActive = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, escalate.StatusCode);
    }

    [IntegrationFact]
    public async Task A_login_payload_cannot_assign_a_role()
    {
        var account = await fixture.NewUser(AppRoles.Salesperson);
        using var client = fixture.Factory.NewClient();
        using var response = await Send(client, HttpMethod.Post, "/api/auth/login", new
        {
            account.UserName, password = fixture.Password, role = AppRoles.Administrator
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await client.GetFromJsonAsync<UserSummary>("/api/auth/me");
        Assert.Equal(AppRoles.Salesperson, session!.Role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
    }

    [IntegrationFact]
    public async Task A_role_change_invalidates_existing_sessions()
    {
        var account = await fixture.NewUser(AppRoles.Salesperson);
        using var client = await fixture.Login(account.UserName);
        using var update = await Send(fixture.Admin, HttpMethod.Put, $"/api/users/{account.Id}", new
        {
            account.DisplayName, role = AppRoles.Warehouse, isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/customers")).StatusCode);
        using var newSession = await fixture.Login(account.UserName);
        Assert.Equal(HttpStatusCode.Forbidden, (await newSession.GetAsync("/api/customers")).StatusCode);
    }

    [IntegrationFact]
    public async Task Disabled_accounts_lose_access_and_cannot_log_in()
    {
        var account = await fixture.NewUser(AppRoles.Accountant);
        using var client = await fixture.Login(account.UserName);
        using var update = await Send(fixture.Admin, HttpMethod.Put, $"/api/users/{account.Id}", new
        {
            account.DisplayName, account.Role, isActive = false
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var rejected = await Send(client, HttpMethod.Post, "/api/auth/login", new { account.UserName, password = fixture.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    [IntegrationFact]
    public async Task An_administrator_cannot_remove_its_own_access()
    {
        using var response = await Send(fixture.Admin, HttpMethod.Put, $"/api/users/{fixture.Administrator.Id}", new
        {
            displayName = "Administrator test", role = AppRoles.Salesperson, isActive = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AppRoles.Administrator, (await fixture.Admin.GetFromJsonAsync<UserSummary>("/api/auth/me"))!.Role);
    }

    [IntegrationFact]
    public async Task Mutations_without_a_CSRF_token_are_rejected()
    {
        using var client = await fixture.Login("admin-tests");
        using var response = await Send(client, HttpMethod.Post, "/api/customers", new { name = "No CSRF" }, withCsrf: false);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [IntegrationFact]
    public async Task Cookies_are_secure_HttpOnly_and_not_exposed_as_JSON_tokens()
    {
        using var client = fixture.Factory.NewClient();
        using var response = await Send(client, HttpMethod.Post, "/api/auth/login", new { userName = "admin-tests", password = fixture.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-mini-erp-auth="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", body, StringComparison.OrdinalIgnoreCase);
    }

    [IntegrationFact]
    public async Task Logout_ends_the_session()
    {
        var account = await fixture.NewUser(AppRoles.Warehouse);
        using var client = await fixture.Login(account.UserName);
        using var logout = await Send(client, HttpMethod.Post, "/api/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [IntegrationFact]
    public async Task User_responses_do_not_contain_password_hashes_or_security_stamps()
    {
        var body = await fixture.Admin.GetStringAsync("/api/users");
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("concurrencyStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    [IntegrationFact]
    public async Task Session_keys_and_users_survive_a_new_application_instance()
    {
        var account = await fixture.NewUser(AppRoles.Accountant);
        using var original = fixture.Factory.NewClient();
        using var login = await Send(original, HttpMethod.Post, "/api/auth/login", new { account.UserName, password = fixture.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-mini-erp-auth=")).Split(';')[0];
        await using var restarted = new RoleFactory(fixture.Connection, fixture.BootstrapToken);
        using var client = restarted.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var user = await client.GetFromJsonAsync<UserSummary>("/api/auth/me");
        Assert.Equal(account.Id, user!.Id);
        Assert.Equal(AppRoles.Accountant, user.Role);
    }

    [IntegrationFact]
    public async Task Repeated_incorrect_passwords_temporarily_lock_the_account()
    {
        var account = await fixture.NewUser(AppRoles.Salesperson);
        await using var isolated = new RoleFactory(fixture.Connection, fixture.BootstrapToken);
        using var client = isolated.NewClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var rejected = await Send(client, HttpMethod.Post, "/api/auth/login", new
            {
                account.UserName, password = "incorrect-test-password"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        }
        using var locked = await Send(client, HttpMethod.Post, "/api/auth/login", new { account.UserName, password = fixture.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [IntegrationFact]
    public async Task Secure_cookies_work_when_TLS_is_terminated_before_the_backend()
    {
        using var client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), HandleCookies = false, AllowAutoRedirect = false
        });
        using var csrf = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var nonce = csrf.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-mini-erp-csrf="));
        Assert.Contains("secure", nonce, StringComparison.OrdinalIgnoreCase);
        var token = await csrf.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { userName = "admin-tests", password = fixture.Password })
        };
        request.Headers.Add("Cookie", nonce.Split(';')[0]);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authentication = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-mini-erp-auth="));
        Assert.Contains("secure", authentication, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", authentication, StringComparison.OrdinalIgnoreCase);
    }

    [IntegrationFact]
    public async Task The_owner_can_change_its_password_and_continue_the_session()
    {
        var account = await fixture.NewUser(AppRoles.Warehouse);
        using var client = await fixture.Login(account.UserName);
        var nextPassword = Guid.NewGuid().ToString("N") + " New!";
        using var changed = await Send(client, HttpMethod.Post, "/api/auth/password", new
        {
            currentPassword = fixture.Password, newPassword = nextPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var other = fixture.Factory.NewClient();
        using var oldPassword = await Send(other, HttpMethod.Post, "/api/auth/login", new { account.UserName, password = fixture.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        using var updated = await fixture.Login(account.UserName, nextPassword);
        Assert.Equal(HttpStatusCode.OK, (await updated.GetAsync("/api/auth/me")).StatusCode);
    }
}
