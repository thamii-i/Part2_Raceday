using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201WithEventDetails()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync("/api/events", ApiHelpers.EventBody("Soweto Test Run"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Soweto Test Run", json.GetProperty("eventName").GetString());
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
        Assert.Equal(organiser.UserId, json.GetProperty("organiserId").GetInt32());
        Assert.Equal("Test Organisation", json.GetProperty("organisationName").GetString());
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Renamed Event",
            description = "Updated description",
            eventDate = ApiHelpers.FutureDate(60),
            location = "Cape Town",
            distanceKm = 21.1,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Renamed Event", json.GetProperty("eventName").GetString());
        Assert.Equal("Walk", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_ThenItIsGone()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var delete = await client.DeleteAsync($"/api/events/{eventId}");
        var get = await client.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteEventThatHasCategoriesEnrollmentsAndResults()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);
        await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:45:00", finishPosition = 1 });

        var delete = await client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        using var client = _factory.CreateClientFor(other.Token);

        var update = await client.PutAsJsonAsync($"/api/events/{eventId}", ApiHelpers.EventBody("Hijacked"));
        var delete = await client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task UpdateOrDelete_UnknownEvent_Returns404()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var update = await client.PutAsJsonAsync("/api/events/999999", ApiHelpers.EventBody());
        var delete = await client.DeleteAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            description = "No name supplied",
            eventDate = ApiHelpers.FutureDate(10),
            location = "Johannesburg",
            distanceKm = 5,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync("/api/events", ApiHelpers.EventBody("Past Event", ApiHelpers.FutureDate(-5)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Swimming",
            description = "Not supported",
            eventDate = ApiHelpers.FutureDate(10),
            location = "Durban",
            distanceKm = 5,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewEvents()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token, "Visible To Participants");
        using var client = _factory.CreateClientFor(participant.Token);

        var list = await client.GetAsync("/api/events");
        var single = await client.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        var events = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(events.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == eventId);
    }

    [Fact]
    public async Task GetEvents_CanFilterByType()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        await _factory.CreateEventAsync(organiser.Token, "A Run Event");
        using var client = _factory.CreateClientFor(organiser.Token);

        var cycle = await client.GetAsync("/api/events?type=Cycle&search=A%20Run%20Event");
        var run = await client.GetAsync("/api/events?type=Run&search=A%20Run%20Event");

        var cycleEvents = await cycle.Content.ReadFromJsonAsync<JsonElement>();
        var runEvents = await run.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cycleEvents.GetArrayLength());
        Assert.True(runEvents.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Organiser_CanViewOnlyTheirOwnEvents()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var mine = await _factory.CreateEventAsync(organiser.Token, "Mine");
        var theirs = await _factory.CreateEventAsync(other.Token, "Theirs");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.GetAsync("/api/events/mine");

        var events = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(events.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == mine);
        Assert.DoesNotContain(events.EnumerateArray(), e => e.GetProperty("eventId").GetInt32() == theirs);
    }

    [Fact]
    public async Task Organiser_CanDefineCategory_AndParticipantCanViewIt()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId, "Under 20");
        using var participantClient = _factory.CreateClientFor(participant.Token);

        var response = await participantClient.GetAsync($"/api/events/{eventId}/categories");
        var all = await participantClient.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(categories.EnumerateArray(), c => c.GetProperty("categoryId").GetInt32() == categoryId);
    }

    [Fact]
    public async Task AddCategory_ReusesExistingCategoryName_ForAnotherEvent()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventA = await _factory.CreateEventAsync(organiser.Token, "Event A");
        var eventB = await _factory.CreateEventAsync(organiser.Token, "Event B");

        var first = await _factory.AddCategoryAsync(organiser.Token, eventA, "Veteran");
        var second = await _factory.AddCategoryAsync(organiser.Token, eventB, "veteran");

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task AddCategory_AlreadyAssignedToEvent_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        await _factory.AddCategoryAsync(organiser.Token, eventId, "Senior");
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new { categoryName = "Senior" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_WithMissingName_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new { description = "No name" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanRemoveUnusedCategoryFromEvent()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId, "Junior");
        using var client = _factory.CreateClientFor(organiser.Token);

        var delete = await client.DeleteAsync($"/api/events/{eventId}/categories/{categoryId}");
        var again = await client.DeleteAsync($"/api/events/{eventId}/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }
}
