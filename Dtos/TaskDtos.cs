using System.Text.Json.Serialization;
using SWD.Entities;

namespace SWD.Dtos;

public sealed record CreateTaskRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("assigneeUserId")] Guid AssigneeUserId,
    [property: JsonPropertyName("groupId")] Guid? GroupId
);

public sealed record UpdateTaskStatusRequest([property: JsonPropertyName("status")] TaskItemStatus Status);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    string? JiraIssueKey,
    Guid AssigneeUserId,
    string? AssigneeUserName,
    Guid? GroupId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

