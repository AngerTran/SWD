namespace SWD.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Lecturer = "Lecturer";
    public const string TeamLeader = "TeamLeader";
    public const string TeamMember = "TeamMember";

    public static readonly string[] All = [Admin, Lecturer, TeamLeader, TeamMember];
}

