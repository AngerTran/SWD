namespace SWD.Entities;

public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public string? JiraIssueKey { get; set; }

    public Guid AssigneeUserId { get; set; }
    public ApplicationUser AssigneeUser { get; set; } = default!;

    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

