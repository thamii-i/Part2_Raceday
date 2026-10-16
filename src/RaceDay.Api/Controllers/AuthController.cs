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
[Route("api/auth")]
[Produces("application/json")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordService _passwords;

    public AuthController(RaceDayDbContext db, ITokenService tokens, IPasswordService passwords)
    {
        _db = db;
        _tokens = tokens;
        _passwords = passwords;
    }

    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Registers a new account as an Organiser or a Participant.",
        Description = "Role must be Organiser or Participant. Organisers must supply organisationName; Participants must supply dateOfBirth and gender (Male, Female or Other). The password is stored only as a salted hash.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Account created; the new profile is returned.", typeof(UserProfileDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed, or the role is not Organiser/Participant.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "An account with this email already exists.", typeof(ProblemDetails))]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = RoleNames.All.FirstOrDefault(r => string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            return Problem(detail: "Role must be either 'Organiser' or 'Participant'.", statusCode: StatusCodes.Status400BadRequest);
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (role == RoleNames.Organiser && string.IsNullOrWhiteSpace(request.OrganisationName))
        {
            return Problem(detail: "Organisers must supply an organisationName.", statusCode: StatusCodes.Status400BadRequest);
        }

        DateOnly dob = default;
        if (role == RoleNames.Participant)
        {
            if (request.DateOfBirth is null || string.IsNullOrWhiteSpace(request.Gender))
            {
                return Problem(detail: "Participants must supply dateOfBirth and gender.", statusCode: StatusCodes.Status400BadRequest);
            }

            dob = request.DateOfBirth.Value;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (dob >= today || dob < today.AddYears(-120))
            {
                return Problem(detail: "Date of birth must be a valid date in the past.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        if (await _db.Organisers.AnyAsync(o => o.Email == email) || await _db.Participants.AnyAsync(p => p.Email == email))
        {
            return Problem(detail: "An account with this email already exists.", statusCode: StatusCodes.Status409Conflict);
        }

        UserProfileDto profile;
        if (role == RoleNames.Organiser)
        {
            var organiser = new Organiser
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                OrganisationName = request.OrganisationName!.Trim(),
                PasswordHash = _passwords.Hash(request.Password)
            };
            _db.Organisers.Add(organiser);
            await _db.SaveChangesAsync();
            profile = organiser.ToProfile();
        }
        else
        {
            var participant = new Participant
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                DateOfBirth = dob,
                Gender = request.Gender!,
                PasswordHash = _passwords.Hash(request.Password)
            };
            _db.Participants.Add(participant);
            await _db.SaveChangesAsync();
            profile = participant.ToProfile();
        }

        return Created("/api/users/me", profile);
    }

    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "Logs in with email and password and returns a JWT bearer token.",
        Description = "The token carries the user's id and role. Send it as 'Authorization: Bearer {token}' on every later request (use the Authorize button in Swagger).")]
    [SwaggerResponse(StatusCodes.Status200OK, "Login succeeded; token, expiry and role returned.", typeof(AuthResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Request body is invalid.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Email or password is incorrect.", typeof(ProblemDetails))]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser is not null && _passwords.Verify(organiser.PasswordHash, request.Password))
        {
            return Ok(BuildResponse(organiser.OrganiserID, organiser.Email, organiser.FirstName, organiser.LastName, RoleNames.Organiser));
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant is not null && _passwords.Verify(participant.PasswordHash, request.Password))
        {
            return Ok(BuildResponse(participant.ParticipantID, participant.Email, participant.FirstName, participant.LastName, RoleNames.Participant));
        }

        return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
    }

    private AuthResponse BuildResponse(int id, string email, string firstName, string lastName, string role)
    {
        var (token, expires) = _tokens.CreateToken(id, email, $"{firstName} {lastName}", role);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            UserId = id,
            Email = email,
            FirstName = firstName,
            Role = role
        };
    }
}
