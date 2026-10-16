using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsJsonAsync("/api/events", ApiHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", ApiHelpers.EventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvents_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(participant.Token);

        var update = await client.PutAsJsonAsync($"/api/events/{eventId}", ApiHelpers.EventBody("Nope"));
        var delete = await client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotManageCategories_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        using var client = _factory.CreateClientFor(participant.Token);

        var create = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Sneaky" });
        var delete = await client.DeleteAsync($"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task OrganiserWhoDoesNotOwnTheEvent_CannotAddCategory_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Sneaky" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInEvent_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/enrollments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyEndpoints_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var enrollments = await client.GetAsync("/api/enrollments/mine");
        var results = await client.GetAsync("/api/results/mine");

        Assert.Equal(HttpStatusCode.Forbidden, enrollments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrollmentsOrResults_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(participant.Token);

        var enrollments = await client.GetAsync($"/api/events/{eventId}/enrollments");
        var results = await client.GetAsync($"/api/events/{eventId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, enrollments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotRecordResults_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:45:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserWhoDoesNotOwnTheEvent_CannotViewItsEnrollments_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.GetAsync($"/api/events/{eventId}/enrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
