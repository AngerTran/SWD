using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWD.Data;
using SWD.Entities;
using SWD.Security;

namespace SWD.Services;

public sealed class JiraService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JiraOptions _options;
    private readonly AppDbContext _db;

    public JiraService(IHttpClientFactory httpClientFactory, IOptions<JiraOptions> options, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _db = db;
    }

    public HttpClient CreateClient(string? baseUrl = null)
    {
        var client = _httpClientFactory.CreateClient(nameof(JiraService));
        client.BaseAddress = new Uri((baseUrl ?? _options.BaseUrl).TrimEnd('/') + "/");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Email}:{_options.ApiToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    /// <summary>
    /// Đồng bộ issues từ Jira (JQL project=KEY) sang TaskItem. Cập nhật nếu đã tồn tại theo JiraIssueKey.
    /// </summary>
    public async Task<(int Added, int Updated)> SyncProjectIssuesToTasksAsync(Guid groupId, Guid defaultAssigneeUserId, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return (0, 0);

        var projectKey = group.JiraProjectKey ?? _options.ProjectKey;
        var jql = $"project = \"{projectKey}\" ORDER BY key ASC";
        var added = 0;
        var updated = 0;
        var startAt = 0;
        const int maxResults = 50;

        var client = CreateClient();
        var existingByKey = await _db.Tasks
            .Where(t => t.GroupId == groupId && t.JiraIssueKey != null)
            .ToDictionaryAsync(t => t.JiraIssueKey!, t => t, ct);

        while (true)
        {
            var url = $"rest/api/3/search?jql={Uri.EscapeDataString(jql)}&startAt={startAt}&maxResults={maxResults}&fields=summary,description,status,assignee";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                break;
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("issues", out var issuesArr) || issuesArr.GetArrayLength() == 0)
                break;
            foreach (var issue in issuesArr.EnumerateArray())
            {
                var key = issue.TryGetProperty("key", out var k) ? k.GetString() : null;
                if (string.IsNullOrEmpty(key)) continue;
                var fields = issue.TryGetProperty("fields", out var f) ? f : (JsonElement?)null;
                var summary = fields?.TryGetProperty("summary", out var s) == true ? s.GetString() ?? "" : "";
                var description = GetDescriptionFromField(fields);
                var statusName = fields?.TryGetProperty("status", out var st) == true && st.TryGetProperty("name", out var sn) ? sn.GetString() : null;
                var assigneeEmail = fields?.TryGetProperty("assignee", out var a) == true && a.TryGetProperty("emailAddress", out var ae) ? ae.GetString() : null;
                var status = MapJiraStatusToTaskItemStatus(statusName);
                var assigneeUserId = defaultAssigneeUserId;
                if (!string.IsNullOrEmpty(assigneeEmail))
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == assigneeEmail, ct);
                    if (user != null) assigneeUserId = user.Id;
                }
                if (existingByKey.TryGetValue(key, out var existing))
                {
                    existing.Title = summary;
                    existing.Description = description;
                    existing.Status = status;
                    existing.AssigneeUserId = assigneeUserId;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    updated++;
                }
                else
                {
                    _db.Tasks.Add(new TaskItem
                    {
                        Title = summary,
                        Description = description,
                        Status = status,
                        JiraIssueKey = key,
                        GroupId = groupId,
                        AssigneeUserId = assigneeUserId,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                    added++;
                }
            }
            startAt += maxResults;
            if (issuesArr.GetArrayLength() < maxResults) break;
        }

        await _db.SaveChangesAsync(ct);
        return (added, updated);
    }

    private static string? GetDescriptionFromField(JsonElement? fields)
    {
        if (fields is null || !fields.Value.TryGetProperty("description", out var desc)) return null;
        if (desc.ValueKind == JsonValueKind.String) return desc.GetString();
        if (desc.ValueKind == JsonValueKind.Object && desc.TryGetProperty("content", out var content))
        {
            var sb = new StringBuilder();
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("content", out var inner))
                {
                    foreach (var span in inner.EnumerateArray())
                    {
                        if (span.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }
        return null;
    }

    private static TaskItemStatus MapJiraStatusToTaskItemStatus(string? statusName)
    {
        if (string.IsNullOrEmpty(statusName)) return TaskItemStatus.Todo;
        var u = statusName.ToUpperInvariant();
        if (u.Contains("DONE") || u.Contains("COMPLETE") || u.Contains("RESOLVED")) return TaskItemStatus.Done;
        if (u.Contains("PROGRESS") || u.Contains("IN PROGRESS")) return TaskItemStatus.InProgress;
        return TaskItemStatus.Todo;
    }
}
