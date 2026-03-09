namespace SWD.Entities;

public class CommitRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Sha { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string AuthorName { get; set; } = default!;
    public string? AuthorEmail { get; set; }
    public DateTimeOffset CommittedAt { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    /// <summary>Nhóm (repo) mà commit này được đồng bộ từ.</summary>
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
}

