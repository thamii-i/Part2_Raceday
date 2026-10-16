using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantCanViewIt()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);

        var create = await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:48:12", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var participantClient = _factory.CreateClientFor(participant.Token);
        var mine = await participantClient.GetAsync("/api/results/mine");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var results = await mine.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(1, results[0].GetProperty("finishPosition").GetInt32());
        Assert.Equal("00:48:12", results[0].GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var runnerOne = await _factory.RegisterAndLoginAsync("Participant");
        var runnerTwo = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrolOne = await _factory.EnrolAsync(runnerOne.Token, eventId, categoryId);
        var enrolTwo = await _factory.EnrolAsync(runnerTwo.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrolOne}/results", new { finishTime = "00:40:00", finishPosition = 1 });
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrolTwo}/results", new { finishTime = "00:45:00", finishPosition = 2 });
        using var client = _factory.CreateClientFor(runnerTwo.Token);

        var response = await client.GetAsync("/api/results/mine");

        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal(runnerTwo.UserId, results[0].GetProperty("participantId").GetInt32());
    }

    [Fact]
    public async Task Organiser_CanViewEventResults_OrderedByPosition()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var runnerOne = await _factory.RegisterAndLoginAsync("Participant");
        var runnerTwo = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrolOne = await _factory.EnrolAsync(runnerOne.Token, eventId, categoryId);
        var enrolTwo = await _factory.EnrolAsync(runnerTwo.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);
        await client.PostAsJsonAsync($"/api/enrollments/{enrolOne}/results", new { finishTime = "00:50:00", finishPosition = 2 });
        await client.PostAsJsonAsync($"/api/enrollments/{enrolTwo}/results", new { finishTime = "00:41:00", finishPosition = 1 });

        var response = await client.GetAsync($"/api/events/{eventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, results.GetArrayLength());
        Assert.Equal(1, results[0].GetProperty("finishPosition").GetInt32());
        Assert.Equal(2, results[1].GetProperty("finishPosition").GetInt32());
    }

    [Fact]
    public async Task RecordResult_Twice_ForSameEnrollment_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);
        await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:48:12", finishPosition = 1 });

        var second = await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:49:00", finishPosition = 2 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithPositionAlreadyTaken_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var runnerOne = await _factory.RegisterAndLoginAsync("Participant");
        var runnerTwo = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrolOne = await _factory.EnrolAsync(runnerOne.Token, eventId, categoryId);
        var enrolTwo = await _factory.EnrolAsync(runnerTwo.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);
        await client.PostAsJsonAsync($"/api/enrollments/{enrolOne}/results", new { finishTime = "00:40:00", finishPosition = 1 });

        var response = await client.PostAsJsonAsync($"/api/enrollments/{enrolTwo}/results", new { finishTime = "00:41:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithZeroFinishTime_Returns400()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);

        var response = await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:00:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserWhoDoesNotOwnTheEvent_CannotRecordResult_Returns403()
    {
        var owner = await _factory.RegisterAndLoginAsync("Organiser");
        var other = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(owner.Token);
        var categoryId = await _factory.AddCategoryAsync(owner.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(other.Token);

        var response = await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:48:12", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotWithdraw_AfterResultRecorded_Returns409()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var organiserClient = _factory.CreateClientFor(organiser.Token);
        await organiserClient.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:48:12", finishPosition = 1 });
        using var participantClient = _factory.CreateClientFor(participant.Token);

        var response = await participantClient.DeleteAsync($"/api/enrollments/{enrollmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteResult()
    {
        var organiser = await _factory.RegisterAndLoginAsync("Organiser");
        var participant = await _factory.RegisterAndLoginAsync("Participant");
        var eventId = await _factory.CreateEventAsync(organiser.Token);
        var categoryId = await _factory.AddCategoryAsync(organiser.Token, eventId);
        var enrollmentId = await _factory.EnrolAsync(participant.Token, eventId, categoryId);
        using var client = _factory.CreateClientFor(organiser.Token);
        var create = await client.PostAsJsonAsync($"/api/enrollments/{enrollmentId}/results", new { finishTime = "00:48:12", finishPosition = 3 });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var resultId = created.GetProperty("resultId").GetInt32();

        var update = await client.PutAsJsonAsync($"/api/results/{resultId}", new { finishTime = "00:47:59", finishPosition = 2 });
        var delete = await client.DeleteAsync($"/api/results/{resultId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, updated.GetProperty("finishPosition").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
