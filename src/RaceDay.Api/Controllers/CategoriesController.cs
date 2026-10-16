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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    [HttpGet("/api/categories")]
    [SwaggerOperation(Summary = "Lists every category known to the system. Available to both roles.")]
    [SwaggerResponse(StatusCodes.Status200OK, "All categories.", typeof(List<CategoryDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _db.Categories.OrderBy(c => c.CategoryName).Select(Projections.CategoryToDto).ToListAsync();
        return Ok(categories);
    }

    [HttpGet("/api/events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "Lists the categories assigned to an event. Available to both roles.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Categories of the event.", typeof(List<CategoryDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    public async Task<IActionResult> GetCategoriesForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
        {
            return NotFound();
        }

        var categories = await _db.EventCategories
            .Where(ec => ec.EventID == eventId)
            .Select(ec => ec.Category)
            .OrderBy(c => c.CategoryID)
            .Select(Projections.CategoryToDto)
            .ToListAsync();
        return Ok(categories);
    }

    [HttpPost("/api/events/{eventId:int}/categories")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(
        Summary = "Defines a category for an event. Only the event's Organiser may do this.",
        Description = "If a category with this name already exists it is reused; otherwise it is created. It is then assigned to the event.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Category assigned to the event.", typeof(CategoryDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "This category is already assigned to the event.", typeof(ProblemDetails))]
    public async Task<IActionResult> AddCategoryToEvent(int eventId, [FromBody] CategoryRequest request)
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

        var name = request.CategoryName.Trim();
        var lower = name.ToLower();
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryName.ToLower() == lower);

        if (category is null)
        {
            category = new Category { CategoryName = name, Description = request.Description?.Trim() };
        }
        else if (await _db.EventCategories.AnyAsync(ec => ec.EventID == eventId && ec.CategoryID == category.CategoryID))
        {
            return Problem(detail: "This category is already assigned to the event.", statusCode: StatusCodes.Status409Conflict);
        }

        _db.EventCategories.Add(new EventCategory { EventID = eventId, Category = category });
        await _db.SaveChangesAsync();

        var dto = await _db.Categories.Where(c => c.CategoryID == category.CategoryID).Select(Projections.CategoryToDto).FirstAsync();
        return Created($"/api/events/{eventId}/categories", dto);
    }

    [HttpDelete("/api/events/{eventId:int}/categories/{categoryId:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [SwaggerOperation(Summary = "Removes a category from an event if nobody has enrolled in it. Only the event's Organiser may do this.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Category removed from the event.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Participant, or an Organiser who does not own the event.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Event does not exist, or the category is not assigned to it.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Participants are enrolled in this category for this event.", typeof(ProblemDetails))]
    public async Task<IActionResult> RemoveCategoryFromEvent(int eventId, int categoryId)
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

        var link = await _db.EventCategories.FirstOrDefaultAsync(ec => ec.EventID == eventId && ec.CategoryID == categoryId);
        if (link is null)
        {
            return NotFound();
        }

        if (await _db.Enrollments.AnyAsync(e => e.EventID == eventId && e.CategoryID == categoryId))
        {
            return Problem(detail: "Participants are enrolled in this category, so it cannot be removed.", statusCode: StatusCodes.Status409Conflict);
        }

        _db.EventCategories.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
