namespace RaceDay.Api.Models;

public class Organiser
{
    public int OrganiserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string OrganisationName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<RaceEvent> Events { get; set; } = new List<RaceEvent>();
}

public class Participant
{
    public int ParticipantID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class RaceEvent
{
    public int EventID { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKM { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserID { get; set; }

    public Organiser Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class EventCategory
{
    public int EventID { get; set; }
    public int CategoryID { get; set; }

    public RaceEvent Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public class Enrollment
{
    public int EnrollmentID { get; set; }
    public int ParticipantID { get; set; }
    public int EventID { get; set; }
    public int CategoryID { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public int BibNumber { get; set; }
    public string Status { get; set; } = "Registered";

    public Participant Participant { get; set; } = null!;
    public RaceEvent Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int ResultID { get; set; }
    public int EnrollmentID { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Enrollment Enrollment { get; set; } = null!;
}
