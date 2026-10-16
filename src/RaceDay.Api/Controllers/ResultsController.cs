using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("/api/enrollments/{enrollmentId:int}/results")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(
        Summary = "Captures the finish time and finishing position for an enrollment. Only the event's Organiser may do this.",
        Description = "Body: finishTime (hh:mm:ss) and finishPosition (1 or higher, unique within the event).")]
    [SwaggerResponse(StatusCodes.Status201Created, "Result recorded.", typeof(ResultDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed (finish time must be greater than zero).", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Enrollment does not exist.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A result already exists for this enrollment, or the position is already taken in this event.", typeof(ProblemDetails))]
    public async Task<IActionResult> CreateResult(int enrollmentId, [FromBody] ResultRequest request)
    {
        var enrollment = await _db.Enrollments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
        if (enrollment is null)
        {
            return NotFound();
        }

        if (enrollment.Event.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        if (request.FinishTime!.Value <= TimeSpan.Zero)
        {
            return Problem(detail: "Finish time must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (await _db.Results.AnyAsync(r => r.EnrollmentID == enrollmentId))
        {
            return Problem(detail: "A result has already been recorded for this enrollment.", statusCode: StatusCodes.Status409Conflict);
        }

        if (await _db.Results.AnyAsync(r => r.Enrollment.EventID == enrollment.EventID && r.FinishPosition == request.FinishPosition))
        {
            return Problem(detail: "This finishing position has already been awarded in this event.", statusCode: StatusCodes.Status409Conflict);
        }

        var result = new RaceResult
        {
            EnrollmentID = enrollmentId,
            FinishTime = request.FinishTime.Value,
            FinishPosition = request.FinishPosition,
            RecordedAt = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var dto = await _db.Results.Where(r => r.ResultID == result.ResultID).Select(Projections.ResultToDto).FirstAsync();
        return Created($"/api/results/{result.ResultID}", dto);
    }

    [HttpPut("/api/results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Corrects a captured result. Only the event's Organiser may do this.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The updated result.", typeof(ResultDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Result does not exist.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The position is already taken by another result in this event.", typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrollment).ThenInclude(e => e.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null)
        {
            return NotFound();
        }

        if (result.Enrollment.Event.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        if (request.FinishTime!.Value <= TimeSpan.Zero)
        {
            return Problem(detail: "Finish time must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        }

        var eventId = result.Enrollment.EventID;
        if (await _db.Results.AnyAsync(r => r.ResultID != id && r.Enrollment.EventID == eventId && r.FinishPosition == request.FinishPosition))
        {
            return Problem(detail: "This finishing position has already been awarded in this event.", statusCode: StatusCodes.Status409Conflict);
        }

        result.FinishTime = request.FinishTime.Value;
        result.FinishPosition = request.FinishPosition;
        await _db.SaveChangesAsync();

        var dto = await _db.Results.Where(r => r.ResultID == id).Select(Projections.ResultToDto).FirstAsync();
        return Ok(dto);
    }

    [HttpDelete("/api/results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Deletes a captured result. Only the event's Organiser may do this.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Result deleted.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Result does not exist.")]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await _db.Results
            .Include(r => r.Enrollment).ThenInclude(e => e.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null)
        {
            return NotFound();
        }

        if (result.Enrollment.Event.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("/api/events/{eventId:int}/results")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Lists all captured results for an event, ordered by finishing position. Only the event's Organiser may do this.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Results for the event.", typeof(List<ResultDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        var list = await _db.Results
            .Where(r => r.Enrollment.EventID == eventId)
            .OrderBy(r => r.FinishPosition)
            .Select(Projections.ResultToDto)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("/api/results/mine")]
    [Authorize(Roles = RoleNames.Participant)]
    [SwaggerOperation(Summary = "Lists the logged-in Participant's own results (personal performance history).")]
    [SwaggerResponse(StatusCodes.Status200OK, "The participant's results, newest event first.", typeof(List<ResultDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as an Organiser.")]
    public async Task<IActionResult> GetMyResults()
    {
        var me = User.GetUserId();
        var list = await _db.Results
            .Where(r => r.Enrollment.ParticipantID == me)
            .OrderByDescending(r => r.Enrollment.Event.EventDate)
            .Select(Projections.ResultToDto)
            .ToListAsync();
        return Ok(list);
    }
}
