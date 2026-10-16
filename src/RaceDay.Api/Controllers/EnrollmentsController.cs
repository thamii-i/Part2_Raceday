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
public class EnrollmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrollmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpPost("/api/events/{eventId:int}/enrollments")]
    [Authorize(Roles = RoleNames.Participant)]
    [SwaggerOperation(
        Summary = "Enters the logged-in Participant into an event by selecting a category.",
        Description = "The category must be assigned to the event. The enrollment records the participant, the event and the chosen category, and a unique bib number is issued.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Enrollment recorded.", typeof(EnrollmentDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Category is not assigned to this event, or the event has already taken place.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as an Organiser.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "The participant is already enrolled in this event.", typeof(ProblemDetails))]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrollmentRequest request)
    {
        var me = User.GetUserId();

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (!await _db.EventCategories.AnyAsync(ec => ec.EventID == eventId && ec.CategoryID == request.CategoryId))
        {
            return Problem(detail: "The selected category is not available for this event.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (ev.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Problem(detail: "This event has already taken place.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (await _db.Enrollments.AnyAsync(e => e.EventID == eventId && e.ParticipantID == me))
        {
            return Problem(detail: "You are already enrolled in this event.", statusCode: StatusCodes.Status409Conflict);
        }

        var highestBib = await _db.Enrollments.MaxAsync(e => (int?)e.BibNumber) ?? 0;

        var enrollment = new Enrollment
        {
            ParticipantID = me,
            EventID = eventId,
            CategoryID = request.CategoryId,
            EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            BibNumber = Math.Max(highestBib, 999) + 1,
            Status = "Registered"
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        var dto = await _db.Enrollments.Where(e => e.EnrollmentID == enrollment.EnrollmentID).Select(Projections.EnrollmentToDto).FirstAsync();
        return Created($"/api/enrollments/{enrollment.EnrollmentID}", dto);
    }

    [HttpGet("/api/events/{eventId:int}/enrollments")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Lists every enrollment for an event. Only the event's Organiser may do this.")]
    [SwaggerResponse(StatusCodes.Status200OK, "All enrollments for the event.", typeof(List<EnrollmentDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> GetEventEnrollments(int eventId)
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

        var list = await _db.Enrollments
            .Where(e => e.EventID == eventId)
            .OrderBy(e => e.EnrollmentID)
            .Select(Projections.EnrollmentToDto)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("/api/enrollments/mine")]
    [Authorize(Roles = RoleNames.Participant)]
    [SwaggerOperation(Summary = "Lists the logged-in Participant's own enrollments.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The participant's enrollments.", typeof(List<EnrollmentDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as an Organiser.")]
    public async Task<IActionResult> GetMyEnrollments()
    {
        var me = User.GetUserId();
        var list = await _db.Enrollments
            .Where(e => e.ParticipantID == me)
            .OrderBy(e => e.Event.EventDate)
            .Select(Projections.EnrollmentToDto)
            .ToListAsync();
        return Ok(list);
    }

    [HttpDelete("/api/enrollments/{id:int}")]
    [Authorize(Roles = RoleNames.Participant)]
    [SwaggerOperation(Summary = "Withdraws the logged-in Participant from an event they enrolled in.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Enrollment removed.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Organiser, or the enrollment belongs to another participant.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Enrollment does not exist.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A result has already been recorded for this enrollment.", typeof(ProblemDetails))]
    public async Task<IActionResult> Withdraw(int id)
    {
        var enrollment = await _db.Enrollments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrollmentID == id);
        if (enrollment is null)
        {
            return NotFound();
        }

        if (enrollment.ParticipantID != User.GetUserId())
        {
            return Forbid();
        }

        if (enrollment.Result is not null)
        {
            return Problem(detail: "A result has already been recorded, so you cannot withdraw.", statusCode: StatusCodes.Status409Conflict);
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
