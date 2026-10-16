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
[Route("api/events")]
[Produces("application/json")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lists events, optionally filtered. Available to both roles.",
        Description = "Optional query parameters: type (Run, Walk or Cycle), upcomingOnly (true = events not yet held) and search (text in the event name or location).")]
    [SwaggerResponse(StatusCodes.Status200OK, "List of events ordered by date.", typeof(List<EventDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    public async Task<IActionResult> GetEvents([FromQuery] EventType? type, [FromQuery] bool upcomingOnly = false, [FromQuery] string? search = null)
    {
        var query = _db.Events.AsQueryable();

        if (type.HasValue)
        {
            query = query.Where(e => e.EventType == type.Value);
        }

        if (upcomingOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term));
        }

        var events = await query.OrderBy(e => e.EventDate).Select(Projections.EventToDto).ToListAsync();
        return Ok(events);
    }

    [HttpGet("mine")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Lists the events created by the logged-in Organiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The organiser's events.", typeof(List<EventDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as a Participant.")]
    public async Task<IActionResult> GetMyEvents()
    {
        var me = User.GetUserId();
        var events = await _db.Events
            .Where(e => e.OrganiserID == me)
            .OrderBy(e => e.EventDate)
            .Select(Projections.EventToDto)
            .ToListAsync();
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Returns a single event. Available to both roles.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The event.", typeof(EventDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var dto = await _db.Events.Where(e => e.EventID == id).Select(Projections.EventToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(
        Summary = "Creates a new event owned by the logged-in Organiser.",
        Description = "Body: eventName, description, eventDate (yyyy-MM-dd, today or later), location, distanceKm and eventType (Run, Walk or Cycle).")]
    [SwaggerResponse(StatusCodes.Status201Created, "Event created.", typeof(EventDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed, or the date is in the past.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Logged in as a Participant.")]
    public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.EventDate!.Value < today)
        {
            return Problem(detail: "Event date cannot be in the past.", statusCode: StatusCodes.Status400BadRequest);
        }

        var ev = new RaceEvent
        {
            OrganiserID = User.GetUserId(),
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate.Value,
            Location = request.Location.Trim(),
            DistanceKM = request.DistanceKm,
            EventType = request.EventType!.Value
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var dto = await _db.Events.Where(e => e.EventID == ev.EventID).Select(Projections.EventToDto).FirstAsync();
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Updates an event. Only the Organiser who created it may do this.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The updated event.", typeof(EventDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own this event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKM = request.DistanceKm;
        ev.EventType = request.EventType!.Value;

        await _db.SaveChangesAsync();

        var dto = await _db.Events.Where(e => e.EventID == id).Select(Projections.EventToDto).FirstAsync();
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Deletes an event together with its category links, enrollments and results. Owner only.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Event deleted.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own this event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == id);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserID != User.GetUserId())
        {
            return Forbid();
        }

        var results = await _db.Results.Where(r => r.Enrollment.EventID == id).ToListAsync();
        var enrollments = await _db.Enrollments.Where(e => e.EventID == id).ToListAsync();
        var links = await _db.EventCategories.Where(c => c.EventID == id).ToListAsync();

        _db.Results.RemoveRange(results);
        _db.Enrollments.RemoveRange(enrollments);
        _db.EventCategories.RemoveRange(links);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
