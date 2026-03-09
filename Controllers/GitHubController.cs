using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWD.Security;
using SWD.Services;

namespace SWD.Controllers;

[ApiController]
[Route("api/github")]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class GitHubController : ControllerBase
{
    private readonly GitHubService _gitHub;

    public GitHubController(GitHubService gitHub) => _gitHub = gitHub;

    [HttpPost("sync-commits")]
    [Authorize(Roles = $"{Roles.Lecturer},{Roles.TeamLeader},{Roles.Admin}")]
    public async Task<ActionResult<object>> SyncCommits([FromQuery] Guid groupId, [FromQuery] int maxPages = 10, CancellationToken ct = default)
    {
        var (added, skipped) = await _gitHub.SyncRepoCommitsAsync(groupId, maxPages, ct);
        return Ok(new { groupId, added, skipped, message = "Đồng bộ GitHub commits xong." });
    }
}

