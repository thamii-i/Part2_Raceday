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
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;

    public UsersController(RaceDayDbContext db, IPasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpGet("me")]
    [SwaggerOperation(Summary = "Returns the profile of the logged-in user (Organiser or Participant).")]
    [SwaggerResponse(StatusCodes.Status200OK, "The user's profile.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account no longer exists.")]
    public async Task<IActionResult> GetMe()
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            return organiser is null ? NotFound() : Ok(organiser.ToProfile());
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        return participant is null ? NotFound() : Ok(participant.ToProfile());
    }

    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Updates the logged-in user's own profile.",
        Description = "Organisers may change names, phone and organisationName. Participants may change names, phone, dateOfBirth and gender.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The updated profile.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "The account no longer exists.")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            if (organiser is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.OrganisationName))
            {
                return Problem(detail: "Organisers must supply an organisationName.", statusCode: StatusCodes.Status400BadRequest);
            }

            organiser.FirstName = request.FirstName.Trim();
            organiser.LastName = request.LastName.Trim();
            organiser.Phone = request.Phone.Trim();
            organiser.OrganisationName = request.OrganisationName.Trim();
            await _db.SaveChangesAsync();
            return Ok(organiser.ToProfile());
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        if (participant is null)
        {
            return NotFound();
        }

        if (request.DateOfBirth is null || string.IsNullOrWhiteSpace(request.Gender))
        {
            return Problem(detail: "Participants must supply dateOfBirth and gender.", statusCode: StatusCodes.Status400BadRequest);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dob = request.DateOfBirth.Value;
        if (dob >= today || dob < today.AddYears(-120))
        {
            return Problem(detail: "Date of birth must be a valid date in the past.", statusCode: StatusCodes.Status400BadRequest);
        }

        participant.FirstName = request.FirstName.Trim();
        participant.LastName = request.LastName.Trim();
        participant.Phone = request.Phone.Trim();
        participant.DateOfBirth = dob;
        participant.Gender = request.Gender;
        await _db.SaveChangesAsync();
        return Ok(participant.ToProfile());
    }

    [HttpPut("me/password")]
    [SwaggerOperation(Summary = "Changes the logged-in user's password after checking the current one.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Password changed.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Current password is wrong or the new password is invalid.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not logged in.")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var id = User.GetUserId();

        if (User.IsInRole(RoleNames.Organiser))
        {
            var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.OrganiserID == id);
            if (organiser is null)
            {
                return Unauthorized();
            }

            if (!_passwords.Verify(organiser.PasswordHash, request.CurrentPassword))
            {
                return Problem(detail: "Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest);
            }

            organiser.PasswordHash = _passwords.Hash(request.NewPassword);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.ParticipantID == id);
        if (participant is null)
        {
            return Unauthorized();
        }

        if (!_passwords.Verify(participant.PasswordHash, request.CurrentPassword))
        {
            return Problem(detail: "Current password is incorrect.", statusCode: StatusCodes.Status400BadRequest);
        }

        participant.PasswordHash = _passwords.Hash(request.NewPassword);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
