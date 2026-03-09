using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SWD.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("Admin")) return RedirectToAction(nameof(Admin));
        if (User.IsInRole("Lecturer")) return RedirectToAction(nameof(Lecturer));
        if (User.IsInRole("TeamLeader")) return RedirectToAction(nameof(TeamLeader));
        if (User.IsInRole("TeamMember")) return RedirectToAction(nameof(TeamMember));
        
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin() => View();

    [Authorize(Roles = "Lecturer")]
    public IActionResult Lecturer() => View();

    [Authorize(Roles = "TeamLeader")]
    public IActionResult TeamLeader() => View();

    [Authorize(Roles = "TeamMember")]
    public IActionResult TeamMember() => View();

    // Functional endpoints for routing to specific pages
    [Authorize(Roles = "Admin,Lecturer,TeamLeader")]
    public IActionResult Groups()
    {
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.IsLecturer = User.IsInRole("Lecturer");
        ViewBag.IsTeamLeader = User.IsInRole("TeamLeader");
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Lecturers() => View();

    [Authorize(Roles = "Lecturer,TeamLeader,Admin")]
    public IActionResult Requirements() => View();

    [Authorize(Roles = "TeamLeader,Admin")]
    public IActionResult Tasks() => View();

    [Authorize(Roles = "TeamMember")]
    public IActionResult MyTasks() => View();

    [Authorize(Roles = "Admin,Lecturer,TeamLeader,TeamMember")]
    public IActionResult Commits()
    {
        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.IsLecturer = User.IsInRole("Lecturer");
        ViewBag.IsTeamLeader = User.IsInRole("TeamLeader");
        ViewBag.IsTeamMember = User.IsInRole("TeamMember");
        return View();
    }

    [Authorize(Roles = "TeamLeader,Admin")]
    public IActionResult Sync() => View();

    [Authorize(Roles = "TeamLeader,Admin")]
    public IActionResult SRS() => View();
}
