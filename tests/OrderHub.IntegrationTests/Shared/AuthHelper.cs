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
        var count = Interlocked.Increment(ref _counter);
        var email = $"test-{count}-{Guid.NewGuid():N}@test.com";
        var password = "Test@12345";
        var phone = $"09{count:D8}";

        if (role == UserRoleEnum.Admin)
        {
            // Register as customer first, then promote via DB
            var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                Email = email,
                Password = password,
                FullName = "Test Admin",
                Phone = phone
            });
            if (!registerResponse.IsSuccessStatusCode)
            {
                var body = await registerResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Admin registration failed with status {registerResponse.StatusCode}. Response: {body}");
            }

            using var scope = fixture.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.Role = UserRoleEnum.Admin;
            await db.SaveChangesAsync();

            // Login again to get token with Admin role
            var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = email,
                Password = password
            });
            loginResponse.EnsureSuccessStatusCode();
            return (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FullName = "Test Customer",
            Phone = phone
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Registration failed with status {response.StatusCode}. Response: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
