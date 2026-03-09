namespace SWD.Entities;

public enum ReportType
{
    Srs = 0,
    Progress = 1
}

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ReportType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

