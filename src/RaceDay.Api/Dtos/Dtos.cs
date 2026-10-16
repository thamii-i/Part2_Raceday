using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Phone { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;
    [StringLength(100)] public string? OrganisationName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [RegularExpression("^(Male|Female|Other)$")] public string? Gender { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? OrganisationName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public class UpdateProfileRequest
{
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Phone { get; set; } = string.Empty;
    [StringLength(100)] public string? OrganisationName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [RegularExpression("^(Male|Female|Other)$")] public string? Gender { get; set; }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
}

public class EventRequest
{
    [Required, StringLength(100)] public string EventName { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Description { get; set; } = string.Empty;
    [Required] public DateOnly? EventDate { get; set; }
    [Required, StringLength(100)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 999)] public decimal DistanceKm { get; set; }
    [Required] public EventType? EventType { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserId { get; set; }
    public string OrganisationName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(50)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(200)] public string? Description { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EnrollmentRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class EnrollmentDto
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int BibNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
}

public class ResultRequest
{
    [Required] public TimeSpan? FinishTime { get; set; }
    [Range(1, 1000000)] public int FinishPosition { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int BibNumber { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; }
}
