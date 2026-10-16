namespace RaceDay.Api.Models;

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Organiser, Participant };
}
