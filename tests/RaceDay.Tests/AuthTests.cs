using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_WithValidDetails_Returns201(string role)
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";

        var response = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, role));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(role, json.GetProperty("role").GetString());
        Assert.Equal(email, json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";

        var first = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Participant"));
        var second = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Participant"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_SameEmailAsAnOrganiserWhenParticipant_Returns409()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";

        var first = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Organiser"));
        var second = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Participant"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithRoleThatIsNotOffered_Returns400()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";

        var response = await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithoutOrganisationName_Returns400()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            firstName = "Test", lastName = "Org", email = $"{Guid.NewGuid():N}@test.raceday.co.za",
            password = ApiHelpers.Password, phone = "0821234567", role = "Organiser"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            firstName = "Test", lastName = "Runner", email = $"{Guid.NewGuid():N}@test.raceday.co.za",
            password = ApiHelpers.Password, phone = "0821234567", role = "Participant", gender = "Male"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            firstName = "Test", lastName = "User", email = $"{Guid.NewGuid():N}@test.raceday.co.za",
            password = "short", phone = "0821234567", role = "Participant", dateOfBirth = "1990-05-01", gender = "Male"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "nobody@test.raceday.co.za" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresPasswordAsHash_NotPlainText()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.raceday.co.za";
        await client.PostAsJsonAsync("/api/auth/register", ApiHelpers.RegistrationBody(email, "Participant"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var participant = await db.Participants.SingleAsync(p => p.Email == email);

        Assert.NotEqual(ApiHelpers.Password, participant.PasswordHash);
        Assert.DoesNotContain(ApiHelpers.Password, participant.PasswordHash);
        Assert.True(participant.PasswordHash.Length > 40);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole(string role)
    {
        var user = await _factory.RegisterAndLoginAsync(role);
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = ApiHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
        Assert.Equal(role, json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var user = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@test.raceday.co.za", password = ApiHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users/me")]
    [InlineData("/api/events")]
    [InlineData("/api/categories")]
    [InlineData("/api/enrollments/mine")]
    [InlineData("/api/results/mine")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string url)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_Returns401()
    {
        using var client = _factory.CreateClientFor("this.is.not-a-valid-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
