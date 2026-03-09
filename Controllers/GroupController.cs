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
[Route("api/groups")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class GroupController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupController(AppDbContext db) => _db = db;

    /// <summary>Admin: tất cả nhóm. Lecturer: chỉ nhóm được gán. TeamLeader: chỉ nhóm của mình.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Lecturer},{Roles.TeamLeader}")]
    public async Task<ActionResult<List<GroupResponse>>> List(CancellationToken ct)
    {
        var query = _db.Groups.AsQueryable();
        if (User.IsInRole(Roles.Lecturer))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var lecturerId = Guid.Parse(uid);
            query = query.Where(g => _db.GroupLecturers.Any(gl => gl.GroupId == g.Id && gl.LecturerUserId == lecturerId));
        }
        else if (User.IsInRole(Roles.TeamLeader) && !User.IsInRole(Roles.Admin))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var userId = Guid.Parse(uid);
            query = query.Where(g => g.Users.Any(u => u.Id == userId));
        }
        var items = await query
            .OrderBy(g => g.Code)
            .Select(g => new GroupResponse(g.Id, g.Code, g.Name, g.JiraProjectKey, g.GitHubRepo))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<GroupResponse>> Create(CreateGroupRequest req, CancellationToken ct)
    {
        var code = req.Code.Trim();
        if (string.IsNullOrEmpty(code))
            return BadRequest(new { message = "Mã nhóm không được để trống." });
        if (await _db.Groups.AnyAsync(g => g.Code == code, ct))
            return BadRequest(new { message = "Mã nhóm \"" + code + "\" đã tồn tại. Vui lòng chọn mã khác." });
        var group = new Group
        {
            Code = code,
            Name = req.Name.Trim(),
            JiraProjectKey = string.IsNullOrWhiteSpace(req.JiraProjectKey) ? null : req.JiraProjectKey.Trim(),
            GitHubRepo = string.IsNullOrWhiteSpace(req.GitHubRepo) ? null : req.GitHubRepo.Trim()
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync(ct);
        return Ok(new GroupResponse(group.Id, group.Code, group.Name, group.JiraProjectKey, group.GitHubRepo));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<GroupResponse>> Update(Guid id, CreateGroupRequest req, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (group is null) return NotFound();
        var code = req.Code.Trim();
        if (string.IsNullOrEmpty(code))
            return BadRequest(new { message = "Mã nhóm không được để trống." });
        if (await _db.Groups.AnyAsync(g => g.Code == code && g.Id != id, ct))
            return BadRequest(new { message = "Mã nhóm \"" + code + "\" đã được nhóm khác sử dụng. Vui lòng chọn mã khác." });
        group.Code = code;
        group.Name = req.Name.Trim();
        group.JiraProjectKey = string.IsNullOrWhiteSpace(req.JiraProjectKey) ? null : req.JiraProjectKey.Trim();
        group.GitHubRepo = string.IsNullOrWhiteSpace(req.GitHubRepo) ? null : req.GitHubRepo.Trim();
        await _db.SaveChangesAsync(ct);
        return Ok(new GroupResponse(group.Id, group.Code, group.Name, group.JiraProjectKey, group.GitHubRepo));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (group is null) return NotFound();

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Danh sách user chưa ở trong nhóm (để Lecturer/Admin thêm thành viên).</summary>
    [HttpGet("{id:guid}/available-users")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Lecturer}")]
    public async Task<ActionResult<List<GroupMemberResponse>>> GetAvailableUsers(Guid id, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([id], ct);
        if (group is null) return NotFound();
        if (User.IsInRole(Roles.Lecturer) && !User.IsInRole(Roles.Admin))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var assigned = await _db.GroupLecturers.AnyAsync(gl => gl.GroupId == id && gl.LecturerUserId == Guid.Parse(uid), ct);
            if (!assigned) return Forbid();
        }
        var users = await _db.Users
            .Where(u => u.GroupId != id)
            .OrderBy(u => u.UserName)
            .Select(u => new GroupMemberResponse(u.Id, u.Email ?? "", u.UserName, u.GroupId))
            .Take(200)
            .ToListAsync(ct);
        return Ok(users);
    }

    /// <summary>Danh sách thành viên nhóm (students/members). Lecturer chỉ xem nhóm được gán.</summary>
    [HttpGet("{id:guid}/members")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Lecturer},{Roles.TeamLeader}")]
    public async Task<ActionResult<List<GroupMemberResponse>>> ListMembers(Guid id, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([id], ct);
        if (group is null) return NotFound();
        if (User.IsInRole(Roles.Lecturer) && !User.IsInRole(Roles.Admin))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var assigned = await _db.GroupLecturers.AnyAsync(gl => gl.GroupId == id && gl.LecturerUserId == Guid.Parse(uid), ct);
            if (!assigned) return Forbid();
        }
        var members = await _db.Users
            .Where(u => u.GroupId == id)
            .Select(u => new GroupMemberResponse(u.Id, u.Email ?? "", u.UserName, u.GroupId))
            .ToListAsync(ct);
        return Ok(members);
    }

    /// <summary>Thêm thành viên vào nhóm (set User.GroupId). Admin hoặc Lecturer (nhóm được gán).</summary>
    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Lecturer}")]
    public async Task<ActionResult<GroupMemberResponse>> AddMember(Guid id, [FromBody] AddMemberRequest req, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([id], ct);
        if (group is null) return NotFound();
        if (User.IsInRole(Roles.Lecturer) && !User.IsInRole(Roles.Admin))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var assigned = await _db.GroupLecturers.AnyAsync(gl => gl.GroupId == id && gl.LecturerUserId == Guid.Parse(uid), ct);
            if (!assigned) return Forbid();
        }
        var user = await _db.Users.FindAsync([req.UserId], ct);
        if (user is null) return NotFound("User not found.");
        user.GroupId = id;
        await _db.SaveChangesAsync(ct);
        return Ok(new GroupMemberResponse(user.Id, user.Email ?? "", user.UserName, user.GroupId));
    }

    /// <summary>Xóa thành viên khỏi nhóm (set GroupId = null).</summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Lecturer}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([id], ct);
        if (group is null) return NotFound();
        if (User.IsInRole(Roles.Lecturer) && !User.IsInRole(Roles.Admin))
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return Forbid();
            var assigned = await _db.GroupLecturers.AnyAsync(gl => gl.GroupId == id && gl.LecturerUserId == Guid.Parse(uid), ct);
            if (!assigned) return Forbid();
        }
        var user = await _db.Users.FindAsync([userId], ct);
        if (user is null || user.GroupId != id) return NotFound();
        user.GroupId = null;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

