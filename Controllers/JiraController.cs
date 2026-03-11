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
[Route("api/jira")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class JiraController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JiraService _jira;

    public JiraController(AppDbContext db, JiraService jira)
    {
        _db = db;
        _jira = jira;
    }

    [HttpPost("sync")]
    [Authorize(Roles = $"{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> Sync([FromQuery] Guid groupId, CancellationToken ct)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null) return NotFound();

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uid is null) return Unauthorized();
        var userId = Guid.Parse(uid);

        // TeamLeader chỉ được sync nhóm mà mình là thành viên (GroupId của user = groupId)
        if (User.IsInRole(Roles.TeamLeader) && !User.IsInRole(Roles.Admin))
        {
            var userInGroup = await _db.Users.AnyAsync(u => u.Id == userId && u.GroupId == groupId, ct);
            if (!userInGroup)
                return StatusCode(403, new { title = "Forbidden", message = "Bạn chỉ được đồng bộ Jira cho nhóm của mình." });
        }

        var (added, updated) = await _jira.SyncProjectIssuesToTasksAsync(groupId, userId, ct);
        return Ok(new { groupId, added, updated, message = "Jira đồng bộ xong." });
    }
}

