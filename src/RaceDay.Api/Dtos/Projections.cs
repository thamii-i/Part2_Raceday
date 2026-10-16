using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public static class Projections
{
    public static readonly Expression<Func<RaceEvent, EventDto>> EventToDto = e => new EventDto
    {
        EventId = e.EventID,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKM,
        EventType = e.EventType,
        OrganiserId = e.OrganiserID,
        OrganisationName = e.Organiser.OrganisationName,
        EnrollmentCount = e.Enrollments.Count()
    };

    public static readonly Expression<Func<Category, CategoryDto>> CategoryToDto = c => new CategoryDto
    {
        CategoryId = c.CategoryID,
        CategoryName = c.CategoryName,
        Description = c.Description
    };

    public static readonly Expression<Func<Enrollment, EnrollmentDto>> EnrollmentToDto = e => new EnrollmentDto
    {
        EnrollmentId = e.EnrollmentID,
        EventId = e.EventID,
        EventName = e.Event.EventName,
        EventDate = e.Event.EventDate,
        ParticipantId = e.ParticipantID,
        ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
        CategoryId = e.CategoryID,
        CategoryName = e.Category.CategoryName,
        BibNumber = e.BibNumber,
        Status = e.Status,
        EnrollmentDate = e.EnrollmentDate
    };

    public static readonly Expression<Func<RaceResult, ResultDto>> ResultToDto = r => new ResultDto
    {
        ResultId = r.ResultID,
        EnrollmentId = r.EnrollmentID,
        EventId = r.Enrollment.EventID,
        EventName = r.Enrollment.Event.EventName,
        EventDate = r.Enrollment.Event.EventDate,
        ParticipantId = r.Enrollment.ParticipantID,
        ParticipantName = r.Enrollment.Participant.FirstName + " " + r.Enrollment.Participant.LastName,
        CategoryName = r.Enrollment.Category.CategoryName,
        BibNumber = r.Enrollment.BibNumber,
        FinishTime = r.FinishTime,
        FinishPosition = r.FinishPosition,
        RecordedAt = r.RecordedAt
    };

    public static UserProfileDto ToProfile(this Organiser o) => new()
    {
        UserId = o.OrganiserID,
        Role = RoleNames.Organiser,
        FirstName = o.FirstName,
        LastName = o.LastName,
        Email = o.Email,
        Phone = o.Phone,
        OrganisationName = o.OrganisationName
    };

    public static UserProfileDto ToProfile(this Participant p) => new()
    {
        UserId = p.ParticipantID,
        Role = RoleNames.Participant,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Email = p.Email,
        Phone = p.Phone,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender
    };
}
