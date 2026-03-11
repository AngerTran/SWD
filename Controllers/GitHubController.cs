using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Data;
using SWD.Security;
using SWD.Services;

namespace SWD.Controllers;

[ApiController]
[Route("api/github")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class GitHubController : ControllerBase
{
    private readonly GitHubService _gitHub;
    private readonly AppDbContext _db;

    public GitHubController(GitHubService gitHub, AppDbContext db)
    {
        _gitHub = gitHub;
        _db = db;
    }

    [HttpPost("sync-commits")]
    [Authorize(Roles = $"{Roles.Lecturer},{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> SyncCommits([FromQuery] Guid groupId, [FromQuery] int maxPages = 10, CancellationToken ct = default)
    {
        var group = await _db.Groups.FindAsync([groupId], ct);
        if (group is null) return NotFound();

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return Unauthorized();
        var userId = Guid.Parse(uid);

        // Phân quyền: Admin = mọi nhóm; Lecturer = chỉ nhóm được gán (GroupLecturer); TeamLeader = chỉ nhóm của mình
        if (User.IsInRole(Roles.Admin))
        { /* allow */ }
        else if (User.IsInRole(Roles.Lecturer) && !User.IsInRole(Roles.Admin))
        {
            var canSync = await _db.GroupLecturers.AnyAsync(gl => gl.GroupId == groupId && gl.LecturerUserId == userId, ct);
            if (!canSync)
                return StatusCode(403, new { title = "Forbidden", message = "Bạn chỉ được đồng bộ GitHub cho các nhóm được gán." });
        }
        else if (User.IsInRole(Roles.TeamLeader))
        {
            var userInGroup = await _db.Users.AnyAsync(u => u.Id == userId && u.GroupId == groupId, ct);
            if (!userInGroup)
                return StatusCode(403, new { title = "Forbidden", message = "Bạn chỉ được đồng bộ GitHub cho nhóm của mình." });
        }

        var (added, skipped) = await _gitHub.SyncRepoCommitsAsync(groupId, maxPages, ct);
        return Ok(new { groupId, added, skipped, message = "Đồng bộ GitHub commits xong." });
    }
}

