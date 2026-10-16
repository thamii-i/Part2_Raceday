using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EnrollmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrollmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrollmentIsCorrectlyRecorded()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId, "Senior");
        using var participantClient = _factory.CreateClientFor(participant.Token);

        var response = await participantClient.PostAsJsonAsync($"/api/events/{eventId}/enrollments", new { categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(eventId, created.GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, created.GetProperty("categoryId").GetInt32());
        Assert.Equal(participant.UserId, created.GetProperty("participantId").GetInt32());
        Assert.Equal("Registered", created.GetProperty("status").GetString());
        Assert.True(created.GetProperty("bibNumber").GetInt32() > 0);

        var mine = await participantClient.GetAsync("/api/enrollments/mine");
        var myList = await mine.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(myList.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == eventId);

        using var organiserClient = _factory.CreateClientFor(organiser.Token);
        var all = await organiserClient.GetAsync($"/api/events/{eventId}/enrollments");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var allList = await all.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(allList.EnumerateArray(), e =>
            e.GetProperty("participantId").GetInt32() == participant.UserId &&
            e.GetProperty("categoryId").GetInt32() == categoryId);
    }

    [Fact]
    public async Task Enrollments_ReceiveDifferentBibNumbers()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var runnerOne = await _factory.RegisterAndLoginAsync("Participant");
        var runnerTwo = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        await _factory.EnrolAsync(runnerOne.Token, eventId, categoryId);
        await _factory.EnrolAsync(runnerTwo.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.GetAsync($"/api/events/{eventId}/enrollments");

        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        var bibs = list.EnumerateArray().Select(e => e.GetProperty("bibNumber").GetInt32()).ToList();
        Assert.Equal(2, bibs.Count);
        Assert.Equal(bibs.Count, bibs.Distinct().Count());
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInTheSameEvent_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(participant.Token);

        var second = await client.PostAsJsonAsync($"/api/events/{eventId}/enrollments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryNotAssignedToTheEvent_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventA = await _factory.CreateEventAsync(organiser.Token, "Event A");
        var eventB = await _factory.CreateEventAsync(organiser.Token, "Event B");
        var categoryOfB = await _factory.AddCategoryAsync(organiser.Token, eventB, "Only For B");
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsJsonAsync($"/api/events/{eventA}/enrollments", new { categoryId = categoryOfB });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_Returns404()
    {
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        using var client = _factory.CreateClientFor(participant.Token);

        var response = await client.PostAsJsonAsync("/api/events/999999/enrollments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanWithdraw_ThenEnrollmentIsGone()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(participant.Token);

        var delete = await client.DeleteAsync($"/api/enrollments/{enrollmentId}");
        var again = await client.DeleteAsync($"/api/enrollments/{enrollmentId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotWithdrawSomeoneElsesEnrollment_Returns403()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var owner = await _factory.RegisterAndLoginAsync("Participant");
        var other = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(owner.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.DeleteAsync($"/api/enrollments/{enrollmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCategory_WithEnrollments_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.DeleteAsync($"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
