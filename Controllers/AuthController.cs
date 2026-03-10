using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SWD.Dtos;
using SWD.Entities;
using SWD.Security;
using SWD.Services;

namespace SWD.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        if (!Roles.All.Contains(req.Role))
        {
            return BadRequest(new { message = "Invalid role." });
        }

        var user = new ApplicationUser
        {
            Email = req.Email,
            UserName = string.IsNullOrWhiteSpace(req.UserName) ? req.Email : req.UserName
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        var roleResult = await _userManager.AddToRoleAsync(user, req.Role);
        if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);

        var token = await _tokenService.CreateAccessTokenAsync(user);
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? req.Role;
        return Ok(new AuthResponse(token, role, user.Email));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized();

        var token = await _tokenService.CreateAccessTokenAsync(user);
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
        return Ok(new AuthResponse(token, role, user.Email));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme + "," + Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await _userManager.FindByIdAsync(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");
        if (user is null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "";
        return Ok(new UserResponse(user.Id, user.Email ?? "", user.UserName, role));
    }

    /// <summary>
    /// Logout cho client dùng JWT (SPA). Server không lưu token nên chỉ cần client xóa token và chuyển về trang login.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult LogoutApi()
    {
        return Ok(new { message = "Đã đăng xuất. Vui lòng xóa token ở phía client và chuyển đến trang đăng nhập." });
    }
}

