using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWD.Data;
using SWD.Entities;
using SWD.Security;

namespace SWD.Services;

public sealed class GitHubService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GitHubOptions _options;
    private readonly AppDbContext _db;

    public GitHubService(IHttpClientFactory httpClientFactory, IOptions<GitHubOptions> options, AppDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _db = db;
    }

    public HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(GitHubService));
        client.BaseAddress = new Uri("https://api.github.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SWD-SWP391-Tool");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        if (!string.IsNullOrWhiteSpace(_options.Token))
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Token);
        return client;
    }

    /// <summary>
    /// Đồng bộ commits từ GitHub repo của nhóm. Map author (email hoặc login) với User trong hệ thống.
    /// </summary>
    public async Task<(int Added, int Skipped)> SyncRepoCommitsAsync(Guid groupId, int maxPages = 10, CancellationToken ct = default)
    {
        var group = await _db.Groups.FindAsync([groupId], ct);
        if (group is null) return (0, 0);

        var repo = group.GitHubRepo ?? $"{_options.Owner}/{_options.Repo}";
        var parts = repo.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return (0, 0);
        var (owner, repoName) = (parts[0], parts[1]);

        var client = CreateClient();
        var added = 0;
        var skipped = 0;
        var page = 1;
        const int perPage = 100;
        var existingShas = await _db.Commits
            .Where(c => c.GroupId == groupId)
            .Select(c => c.Sha)
            .ToHashSetAsync(ct);
        var usersByEmail = await _db.Users
            .Where(u => u.GroupId == groupId || u.Email != null)
            .ToDictionaryAsync(u => (u.Email ?? "").ToLowerInvariant(), u => u.Id, ct);
        var usersByGitHub = await _db.Users
            .Where(u => u.GitHubUsername != null)
            .ToDictionaryAsync(u => (u.GitHubUsername ?? "").ToLowerInvariant(), u => u.Id, ct);

        while (page <= maxPages)
        {
            var url = $"repos/{owner}/{repoName}/commits?per_page={perPage}&page={page}";
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) break;
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() == 0) break;
            foreach (var commit in arr.EnumerateArray())
            {
                var sha = commit.TryGetProperty("sha", out var s) ? s.GetString() : null;
                if (string.IsNullOrEmpty(sha) || existingShas.Contains(sha)) { skipped++; continue; }
                var commitNode = commit.TryGetProperty("commit", out var cn) ? cn : (JsonElement?)null;
                var message = commitNode?.TryGetProperty("message", out var msg) == true ? msg.GetString() ?? "" : "";
                var author = commitNode?.TryGetProperty("author", out var a) == true ? a : (JsonElement?)null;
                var authorName = author?.TryGetProperty("name", out var n) == true ? n.GetString() ?? "" : "";
                var authorEmail = author?.TryGetProperty("email", out var e) == true ? e.GetString() : null;
                var dateStr = author?.TryGetProperty("date", out var d) == true ? d.GetString() : null;
                var committedAt = DateTimeOffset.UtcNow;
                if (!string.IsNullOrEmpty(dateStr) && DateTimeOffset.TryParse(dateStr, out var parsed)) committedAt = parsed;
                var login = commit.TryGetProperty("author", out var au) && au.ValueKind != JsonValueKind.Null && au.TryGetProperty("login", out var lg) ? lg.GetString() : null;
                var userId = ResolveUserId(authorEmail, login, usersByEmail, usersByGitHub, groupId);
                if (userId == Guid.Empty) userId = await GetOrCreateAnonymousUserIdAsync(groupId, authorName, authorEmail, ct);
                _db.Commits.Add(new CommitRecord
                {
                    Sha = sha,
                    Message = message.Length > 500 ? message[..500] : message,
                    AuthorName = authorName,
                    AuthorEmail = authorEmail,
                    CommittedAt = committedAt,
                    UserId = userId,
                    GroupId = groupId
                });
                existingShas.Add(sha);
                added++;
            }
            if (arr.GetArrayLength() < perPage) break;
            page++;
        }

        await _db.SaveChangesAsync(ct);
        return (added, skipped);
    }

    private static Guid ResolveUserId(string? email, string? login,
        Dictionary<string, Guid> byEmail, Dictionary<string, Guid> byGitHub, Guid groupId)
    {
        if (!string.IsNullOrEmpty(email) && byEmail.TryGetValue(email.ToLowerInvariant(), out var id)) return id;
        if (!string.IsNullOrEmpty(login) && byGitHub.TryGetValue(login.ToLowerInvariant(), out id)) return id;
        return Guid.Empty;
    }

    private async Task<Guid> GetOrCreateAnonymousUserIdAsync(Guid groupId, string authorName, string? authorEmail, CancellationToken ct)
    {
        var fallback = await _db.Users
            .Where(u => u.GroupId == groupId)
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);
        if (fallback != Guid.Empty) return fallback;
        return await _db.Users.Select(u => u.Id).FirstOrDefaultAsync(ct);
    }
}
