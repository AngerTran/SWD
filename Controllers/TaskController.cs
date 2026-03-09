using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Data;
using SWD.Dtos;
using SWD.Entities;
using SWD.Security;

namespace SWD.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _db;

    public TaskController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<TaskResponse>>> List([FromQuery] Guid? groupId, CancellationToken ct)
    {
        var query = _db.Tasks.AsQueryable();
        if (groupId is not null) query = query.Where(t => t.GroupId == groupId);

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TaskResponse(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.JiraIssueKey,
                t.AssigneeUserId,
                t.GroupId,
                t.CreatedAt,
                t.UpdatedAt
            ))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _db.Tasks
            .Where(t => t.Id == id)
            .Select(t => new TaskResponse(t.Id, t.Title, t.Description, t.Status, t.JiraIssueKey, t.AssigneeUserId, t.GroupId, t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync(ct);
        if (task is null) return NotFound();
        return Ok(task);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest req, CancellationToken ct)
    {
        var task = new TaskItem
        {
            Title = req.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            AssigneeUserId = req.AssigneeUserId,
            GroupId = req.GroupId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return Ok(new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.JiraIssueKey, task.AssigneeUserId, task.GroupId, task.CreatedAt, task.UpdatedAt));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = $"{Roles.TeamMember},{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(Guid id, UpdateTaskStatusRequest req, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null) return NotFound();

        // Team member can only update their own tasks.
        var isMemberOnly = User.IsInRole(Roles.TeamMember) && !User.IsInRole(Roles.TeamLeader) && !User.IsInRole(Roles.Admin);
        if (isMemberOnly)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null || task.AssigneeUserId != Guid.Parse(uid)) return Forbid();
        }

        task.Status = req.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new TaskResponse(task.Id, task.Title, task.Description, task.Status, task.JiraIssueKey, task.AssigneeUserId, task.GroupId, task.CreatedAt, task.UpdatedAt));
    }
}

