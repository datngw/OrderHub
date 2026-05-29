using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Auth;
using OrderHub.Domain.Users;
using OrderHub.Infrastructure.Persistence;

namespace OrderHub.IntegrationTests.Shared;

public static class AuthHelper
{
    private static int _counter;

    public static async Task<HttpClient> CreateAuthenticatedCustomerAsync(this IntegrationTestFixture fixture)
    {
        var client = fixture.CreateClient();
        var auth = await RegisterUserAsync(fixture, client, UserRoleEnum.Customer);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<HttpClient> CreateAuthenticatedAdminAsync(this IntegrationTestFixture fixture)
    {
        var client = fixture.CreateClient();
        var auth = await RegisterUserAsync(fixture, client, UserRoleEnum.Admin);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private static async Task<AuthResponse> RegisterUserAsync(
        IntegrationTestFixture fixture, HttpClient client, UserRoleEnum role)
    {
        var email = $"test-{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@test.com";
        var password = "Test@12345";

        if (role == UserRoleEnum.Admin)
        {
            // Register as customer first, then promote via DB
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                FullName = "Test Admin"
            });
            registerResponse.EnsureSuccessStatusCode();

            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.Role = UserRoleEnum.Admin;
            await db.SaveChangesAsync();

            return auth!;
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FullName = "Test Customer"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
