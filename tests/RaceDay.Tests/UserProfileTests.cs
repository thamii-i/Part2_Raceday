using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class UserProfileTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public UserProfileTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task GetMe_ReturnsOwnProfile_ForBothRoles(string role)
    {
        var user = await _factory.RegisterAndLoginAsync(role);
        using var client = _factory.CreateClientFor(user.Token);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.UserId, json.GetProperty("userId").GetInt32());
        Assert.Equal(role, json.GetProperty("role").GetString());
        Assert.Equal(user.Email, json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Participant_CanUpdateOwnProfile()
    {
        var user = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(user.Token);

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Name",
            phone = "0831112222",
            dateOfBirth = "1991-02-03",
            gender = "Female"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated", json.GetProperty("firstName").GetString());
        Assert.Equal("Female", json.GetProperty("gender").GetString());
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnProfile()
    {
        var user = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(user.Token);

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Organiser",
            phone = "0831112222",
            organisationName = "New Organisation"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Organisation", json.GetProperty("organisationName").GetString());
    }

    [Fact]
    public async Task UpdateMe_WithInvalidBody_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(user.Token);

        var response = await client.PutAsJsonAsync("/api/users/me", new { firstName = "", lastName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task ChangePassword_ThenLoginWithNewPassword_Succeeds(string role)
    {
        var user = await _factory.RegisterAndLoginAsync(role);
        using var client = _factory.CreateClientFor(user.Token);

        var change = await client.PutAsJsonAsync("/api/users/me/password", new { currentPassword = ApiHelpers.Password, newPassword = "NewPassword456!" });
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "NewPassword456!" });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        var user = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(user.Token);

        var response = await client.PutAsJsonAsync("/api/users/me/password", new { currentPassword = "Wrong123456!", newPassword = "NewPassword456!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
