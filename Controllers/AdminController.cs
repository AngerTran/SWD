using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWD.Data;
using SWD.Dtos;
using SWD.Entities;
using SWD.Security;

namespace SWD.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin, AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public AdminController(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("lecturers")]
    public async Task<ActionResult<List<UserResponse>>> ListLecturers()
    {
        var users = await _userManager.GetUsersInRoleAsync(Roles.Lecturer);
        var response = users.Select(u => new UserResponse(u.Id, u.Email!, u.UserName, Roles.Lecturer)).ToList();
        return Ok(response);
    }

    [HttpPost("lecturers")]
    public async Task<ActionResult<UserResponse>> CreateLecturer(RegisterRequest req)
    {
        var user = new ApplicationUser
        {
            Email = req.Email,
            UserName = string.IsNullOrWhiteSpace(req.UserName) ? req.Email : req.UserName
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, Roles.Lecturer);

        return Ok(new UserResponse(user.Id, user.Email, user.UserName, Roles.Lecturer));
    }

    [HttpDelete("lecturers/{id:guid}")]
    public async Task<IActionResult> DeleteLecturer(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Lecturer)) return BadRequest("User is not a lecturer.");

        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    /// <summary>Gán giảng viên vào nhóm (assign lecturers to groups).</summary>
    [HttpPost("groups/{groupId:guid}/lecturers/{lecturerUserId:guid}")]
    public async Task<ActionResult<object>> AssignLecturerToGroup(Guid groupId, Guid lecturerUserId, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([groupId], ct);
        if (group is null) return NotFound("Group not found.");
        var lecturer = await _userManager.FindByIdAsync(lecturerUserId.ToString());
        if (lecturer is null) return NotFound("Lecturer not found.");
        var roles = await _userManager.GetRolesAsync(lecturer);
        if (!roles.Contains(Roles.Lecturer)) return BadRequest("User is not a lecturer.");
        if (await _db.GroupLecturers.AnyAsync(x => x.GroupId == groupId && x.LecturerUserId == lecturerUserId, ct))
            return BadRequest("Lecturer already assigned to this group.");
        _db.GroupLecturers.Add(new GroupLecturer { GroupId = groupId, LecturerUserId = lecturerUserId });
        await _db.SaveChangesAsync(ct);
        return Ok(new { groupId, lecturerUserId, message = "Lecturer assigned to group." });
    }

    /// <summary>Bỏ gán giảng viên khỏi nhóm.</summary>
    [HttpDelete("groups/{groupId:guid}/lecturers/{lecturerUserId:guid}")]
    public async Task<IActionResult> UnassignLecturerFromGroup(Guid groupId, Guid lecturerUserId, CancellationToken ct)
    {
        var gl = await _db.GroupLecturers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.LecturerUserId == lecturerUserId, ct);
        if (gl is null) return NotFound();
        _db.GroupLecturers.Remove(gl);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Danh sách giảng viên được gán vào nhóm.</summary>
    [HttpGet("groups/{groupId:guid}/lecturers")]
    public async Task<ActionResult<List<UserResponse>>> ListLecturersOfGroup(Guid groupId, CancellationToken ct)
    {
        var group = await _db.Groups.FindAsync([groupId], ct);
        if (group is null) return NotFound();
        var userIds = await _db.GroupLecturers.Where(x => x.GroupId == groupId).Select(x => x.LecturerUserId).ToListAsync(ct);
        var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(ct);
        var list = users.Select(u => new UserResponse(u.Id, u.Email ?? "", u.UserName, Roles.Lecturer)).ToList();
        return Ok(list);
    }

    /// <summary>Cập nhật GitHub username để map commit author với user (đồng bộ GitHub).</summary>
    [HttpPatch("users/{userId:guid}/github-username")]
    public async Task<ActionResult<object>> SetUserGitHubUsername(Guid userId, [FromBody] SetGitHubUsernameRequest req, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound();
        user.GitHubUsername = string.IsNullOrWhiteSpace(req?.GitHubUsername) ? null : req.GitHubUsername!.Trim();
        await _userManager.UpdateAsync(user);
        return Ok(new { userId, githubUsername = user.GitHubUsername });
    }
}
