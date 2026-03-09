using System.Text.Json.Serialization;

namespace SWD.Dtos;

public sealed record CreateGroupRequest(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("jiraProjectKey")] string? JiraProjectKey,
    [property: JsonPropertyName("githubRepo")] string? GitHubRepo
);
public sealed record GroupResponse(Guid Id, string Code, string Name, string? JiraProjectKey, string? GitHubRepo);

public sealed record GroupMemberResponse(Guid Id, string Email, string? UserName, Guid? GroupId);
public sealed record AddMemberRequest([property: System.Text.Json.Serialization.JsonPropertyName("userId")] Guid UserId);

