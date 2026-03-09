using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Data;
using SWD.Entities;
using SWD.Security;

namespace SWD.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db) => _db = db;

    [HttpGet("progress")]
    [Authorize(Roles = $"{Roles.Lecturer},{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> Progress([FromQuery] Guid groupId, CancellationToken ct)
    {
        var total = await _db.Tasks.CountAsync(t => t.GroupId == groupId, ct);
        var done = await _db.Tasks.CountAsync(t => t.GroupId == groupId && t.Status == TaskItemStatus.Done, ct);
        var inProgress = await _db.Tasks.CountAsync(t => t.GroupId == groupId && t.Status == TaskItemStatus.InProgress, ct);
        var todo = total - done - inProgress;

        return Ok(new
        {
            groupId,
            total,
            todo,
            inProgress,
            done
        });
    }

    [HttpPost("srs")]
    [Authorize(Roles = $"{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> GenerateSrs([FromQuery] Guid groupId, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return NotFound();

        var tasks = await _db.Tasks
            .Where(t => t.GroupId == groupId)
            .OrderBy(t => t.CreatedAt)
            .Include(t => t.AssigneeUser)
            .ToListAsync(ct);

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return Unauthorized();

        var statusText = new[] { "Todo", "In Progress", "Done" };
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SRS (Đặc tả Yêu cầu Phần mềm) - Tự sinh từ dữ liệu hệ thống ===");
        sb.AppendLine();
        sb.AppendLine("1. THÔNG TIN NHÓM (bảng Groups)");
        sb.AppendLine($"   - Mã nhóm: {group.Code}");
        sb.AppendLine($"   - Tên nhóm: {group.Name}");
        sb.AppendLine($"   - Jira Project Key: {group.JiraProjectKey ?? "(chưa cấu hình)"}");
        sb.AppendLine($"   - GitHub Repo: {group.GitHubRepo ?? "(chưa cấu hình)"}");
        sb.AppendLine();
        sb.AppendLine($"2. THỜI GIAN TẠO: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)");
        sb.AppendLine();
        sb.AppendLine("3. YÊU CẦU / CÔNG VIỆC (bảng Tasks, gán người từ bảng Users)");
        sb.AppendLine();
        var idx = 1;
        foreach (var t in tasks)
        {
            var assigneeName = t.AssigneeUser?.UserName ?? t.AssigneeUser?.Email ?? "(chưa gán)";
            var status = (int)t.Status >= 0 && (int)t.Status < statusText.Length ? statusText[(int)t.Status] : t.Status.ToString();
            sb.AppendLine($"   [{idx}] {t.Title}");
            sb.AppendLine($"       Trạng thái: {status}");
            if (!string.IsNullOrWhiteSpace(t.JiraIssueKey)) sb.AppendLine($"       Jira: {t.JiraIssueKey}");
            sb.AppendLine($"       Người thực hiện: {assigneeName}");
            if (!string.IsNullOrWhiteSpace(t.Description)) sb.AppendLine($"       Mô tả: {t.Description}");
            sb.AppendLine();
            idx++;
        }
        sb.AppendLine("--- Hết SRS ---");

        var report = new Report
        {
            Type = ReportType.Srs,
            Title = $"SRS - {group.Code}",
            Content = sb.ToString(),
            GroupId = groupId,
            CreatedByUserId = Guid.Parse(uid)
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(ct);

        return Ok(new { report.Id, report.Title, report.CreatedAt });
    }

    [HttpGet("personal-stats")]
    [Authorize(Roles = Roles.TeamMember)]
    public async Task<ActionResult<object>> PersonalStats(CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return Unauthorized();
        var userId = Guid.Parse(uid);

        var totalTasks = await _db.Tasks.CountAsync(t => t.AssigneeUserId == userId, ct);
        var doneTasks = await _db.Tasks.CountAsync(t => t.AssigneeUserId == userId && t.Status == TaskItemStatus.Done, ct);
        var inProgressTasks = await _db.Tasks.CountAsync(t => t.AssigneeUserId == userId && t.Status == TaskItemStatus.InProgress, ct);
        var commits = await _db.Commits.CountAsync(c => c.UserId == userId, ct);

        return Ok(new
        {
            totalTasks,
            doneTasks,
            inProgressTasks,
            totalCommits = commits
        });
    }

    [HttpGet("commits")]
    [Authorize]
    public async Task<ActionResult<List<object>>> ListCommits([FromQuery] Guid? groupId, [FromQuery] Guid? userId, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var query = _db.Commits.AsQueryable();
        if (userId is not null)
            query = query.Where(c => c.UserId == userId);
        else if (groupId is not null)
            query = query.Where(c => c.GroupId == groupId);
        var items = await query
            .OrderByDescending(c => c.CommittedAt)
            .Take(limit)
            .Select(c => new { c.Id, c.Sha, c.Message, c.AuthorName, c.AuthorEmail, c.CommittedAt, c.UserId, c.GroupId })
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Thống kê commit theo nhóm (số commit theo từng thành viên). Lecturer / Team Leader.</summary>
    [HttpGet("commit-stats")]
    [Authorize(Roles = $"{Roles.Lecturer},{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> CommitStatsByGroup([FromQuery] Guid groupId, CancellationToken ct = default)
    {
        var group = await _db.Groups.FindAsync([groupId], ct);
        if (group is null) return NotFound();
        var byUser = await _db.Commits
            .Where(c => c.GroupId == groupId)
            .GroupBy(c => new { c.UserId })
            .Select(g => new { g.Key.UserId, Count = g.Count() })
            .ToListAsync(ct);
        var userIds = byUser.Select(x => x.UserId).Distinct().ToList();
        var users = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => new { u.Email, u.UserName }, ct);
        var list = byUser.Select(x => new
        {
            x.UserId,
            Email = users.GetValueOrDefault(x.UserId)?.Email ?? "",
            UserName = users.GetValueOrDefault(x.UserId)?.UserName,
            CommitCount = x.Count
        }).OrderByDescending(x => x.CommitCount).ToList();
        var total = list.Sum(x => x.CommitCount);
        return Ok(new { groupId, groupCode = group.Code, byUser = list, totalCommits = total });
    }

    /// <summary>Commit theo tuần (để vẽ biểu đồ). Trả về theo nhóm hoặc theo user.</summary>
    /// <summary>Danh sách báo cáo (SRS, Progress). Có thể lọc theo groupId.</summary>
    [HttpGet("list")]
    [Authorize]
    public async Task<ActionResult<List<object>>> ListReports([FromQuery] Guid? groupId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var query = _db.Reports.AsQueryable();
        if (groupId.HasValue)
            query = query.Where(r => r.GroupId == groupId.Value);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new { r.Id, r.Type, r.Title, r.GroupId, r.CreatedAt })
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("commits-by-week")]
    [Authorize]
    public async Task<ActionResult<object>> CommitsByWeek([FromQuery] Guid? groupId, [FromQuery] Guid? userId, [FromQuery] int weeks = 8, CancellationToken ct = default)
    {
        var query = _db.Commits.AsQueryable();
        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);
        else if (groupId.HasValue)
            query = query.Where(c => c.GroupId == groupId.Value);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-weeks * 7);
        var list = await query
            .Where(c => c.CommittedAt >= cutoff)
            .Select(c => new { c.UserId, c.AuthorName, c.CommittedAt })
            .ToListAsync(ct);
        var start = DateTimeOffset.UtcNow.AddDays(-weeks * 7);
        var labels = new List<string>();
        for (var i = 0; i < weeks; i++)
        {
            var w = start.AddDays(i * 7);
            labels.Add("W" + (i + 1) + " " + w.ToString("dd/MM"));
        }
        var userGroups = list.GroupBy(x => new { x.UserId, x.AuthorName }).ToList();
        var datasets = new List<object>();
        foreach (var ug in userGroups)
        {
            var data = new int[weeks];
            foreach (var c in ug)
            {
                var daysSince = (c.CommittedAt - start).TotalDays;
                var idx = (int)(daysSince / 7);
                if (idx >= 0 && idx < weeks) data[idx]++;
            }
            datasets.Add(new { userId = ug.Key.UserId, userName = ug.Key.AuthorName ?? "?", data });
        }
        return Ok(new { labels, datasets });
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<object>> GetReport(Guid id, [FromQuery] bool download, CancellationToken ct = default)
    {
        var report = await _db.Reports
            .Where(r => r.Id == id)
            .Select(r => new { r.Id, r.Type, r.Title, r.Content, r.GroupId, r.CreatedAt })
            .FirstOrDefaultAsync(ct);
        if (report is null) return NotFound();
        if (download && report.Type == ReportType.Srs)
        {
            var fileName = $"SRS-{report.Title}-{report.CreatedAt.ToString("yyyyMMdd")}.txt";
            var bytes = System.Text.Encoding.UTF8.GetBytes(report.Content);
            return File(bytes, "text/plain", fileName);
        }
        return Ok(report);
    }
}

