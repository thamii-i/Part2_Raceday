using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Tests;

public record TestUser(int UserId, string Email, string Token);

public static class ApiHelpers
{
    public const string Password = "Password123!";

    public static HttpClient CreateClientFor(this WebApplicationFactory<Program> factory, string? token = null)
    {
        var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public static object RegistrationBody(string email, string role)
    {
        if (role == "Organiser")
        {
            return new
            {
                firstName = "Test",
                lastName = "Organiser",
                email,
                password = Password,
                phone = "0821234567",
                role,
                organisationName = "Test Organisation"
            };
        }

        return new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = Password,
            phone = "0821234567",
            role,
            dateOfBirth = "1990-05-01",
            gender = "Male"
        };
    }

    public static async Task<TestUser> RegisterAndLoginAsync(this WebApplicationFactory<Program> factory, string role)
    {
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";
        using var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email, role));
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new TestUser(json.GetProperty("userId").GetInt32(), email, json.GetProperty("token").GetString()!);
    }

    public static string FutureDate(int days = 30) => DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-dd");

    public static object EventBody(string name = "Test 10km Run", string? date = null) => new
    {
        eventName = name,
        description = "A test event",
        eventDate = date ?? FutureDate(),
        location = "Johannesburg",
        distanceKm = 10,
        eventType = "Run"
    };

    public static async Task<int> CreateEventAsync(this WebApplicationFactory<Program> factory, string organiserToken, string name = "Test 10km Run")
    {
        using var client = factory.CreateClientFor(organiserToken);
        var response = await client.PostAsJsonAsync("/api/events", EventBody(name));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("eventId").GetInt32();
    }

    public static async Task<int> AddCategoryAsync(this WebApplicationFactory<Program> factory, string organiserToken, int eventId, string name = "Open")
    {
        using var client = factory.CreateClientFor(organiserToken);
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = name, description = "Test category" });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> EnrolAsync(this WebApplicationFactory<Program> factory, string participantToken, int eventId, int categoryId)
    {
        using var client = factory.CreateClientFor(participantToken);
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/enrollments", new { categoryId });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("enrollmentId").GetInt32();
    }
}
