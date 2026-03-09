using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SWD.Entities;
using SWD.Security;

namespace SWD.Data;

public static class SeedExtensions
{
    public static async Task SeedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(role));
            }
        }
    }

    public static async Task SeedUsersAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var usersToSeed = new (string Email, string Password, string Role, string? UserName)[]
        {
            ("admin@fpt.edu.vn", "Admin@123", Roles.Admin, "Admin"),
            ("lecturer@fpt.edu.vn", "Lecturer@123", Roles.Lecturer, "GV Nguyễn Văn A"),
            ("lecturer2@fpt.edu.vn", "Lecturer@123", Roles.Lecturer, "GV Trần Thị B"),
            ("leader@fpt.edu.vn", "Leader@123", Roles.TeamLeader, "Leader Nhóm 1"),
            ("leader2@fpt.edu.vn", "Leader@123", Roles.TeamLeader, "Leader Nhóm 2"),
            ("member@fpt.edu.vn", "Member@123", Roles.TeamMember, "SV Nguyễn Văn X"),
            ("member2@fpt.edu.vn", "Member@123", Roles.TeamMember, "SV Lê Thị Y"),
            ("member3@fpt.edu.vn", "Member@123", Roles.TeamMember, "SV Phạm Văn Z"),
            ("member4@fpt.edu.vn", "Member@123", Roles.TeamMember, "SV Hoàng Thị T"),
        };

        foreach (var (email, password, role, userName) in usersToSeed)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userName ?? email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }

    /// <summary>Seed dữ liệu mẫu: mỗi bảng 10 bản ghi (Groups, Tasks, Commits, Reports).</summary>
    public static async Task SeedSampleDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await db.Groups.AnyAsync())
            return;

        var admin = await userManager.FindByEmailAsync("admin@fpt.edu.vn");
        var lecturer = await userManager.FindByEmailAsync("lecturer@fpt.edu.vn");
        var lecturer2 = await userManager.FindByEmailAsync("lecturer2@fpt.edu.vn");
        var leader = await userManager.FindByEmailAsync("leader@fpt.edu.vn");
        var leader2 = await userManager.FindByEmailAsync("leader2@fpt.edu.vn");
        var member = await userManager.FindByEmailAsync("member@fpt.edu.vn");
        var member2 = await userManager.FindByEmailAsync("member2@fpt.edu.vn");
        var member3 = await userManager.FindByEmailAsync("member3@fpt.edu.vn");
        var member4 = await userManager.FindByEmailAsync("member4@fpt.edu.vn");

        if (lecturer == null || leader == null || member == null)
            return;

        // Bảng Groups: 10 nhóm
        var g1 = new Group { Code = "G01", Name = "Nhóm 1 - Web bán hàng", JiraProjectKey = "WEB", GitHubRepo = "fpu/swd-web1" };
        var g2 = new Group { Code = "G02", Name = "Nhóm 2 - App mobile", JiraProjectKey = "APP", GitHubRepo = "fpu/swd-app1" };
        var g3 = new Group { Code = "G03", Name = "GameAsset", JiraProjectKey = "GAME", GitHubRepo = "fpu/game-asset" };
        var g4 = new Group { Code = "G04", Name = "SWP391 - Project 01", JiraProjectKey = "SWP", GitHubRepo = "fpu/swp391-p01" };
        var g5 = new Group { Code = "G05", Name = "SWP391 - Project 02", JiraProjectKey = "SWP2", GitHubRepo = "fpu/swp391-p02" };
        var g6 = new Group { Code = "G06", Name = "Nhóm 6 - E-commerce", JiraProjectKey = "ECO", GitHubRepo = "fpu/eco-web" };
        var g7 = new Group { Code = "G07", Name = "Nhóm 7 - Quản lý kho", JiraProjectKey = "INV", GitHubRepo = "fpu/inv-app" };
        var g8 = new Group { Code = "G08", Name = "Nhóm 8 - Hệ thống đặt phòng", JiraProjectKey = "BOOK", GitHubRepo = "fpu/booking" };
        var g9 = new Group { Code = "G09", Name = "Nhóm 9 - Chat app", JiraProjectKey = "CHAT", GitHubRepo = "fpu/chat-app" };
        var g10 = new Group { Code = "G10", Name = "Nhóm 10 - Dashboard analytics", JiraProjectKey = "DASH", GitHubRepo = "fpu/dashboard" };

        db.Groups.AddRange(g1, g2, g3, g4, g5, g6, g7, g8, g9, g10);
        await db.SaveChangesAsync();

        if (leader != null) { leader.GroupId = g1.Id; }
        if (member != null) { member.GroupId = g1.Id; }
        if (member2 != null) { member2.GroupId = g1.Id; }
        if (leader2 != null) { leader2.GroupId = g2.Id; }
        if (member3 != null) { member3.GroupId = g2.Id; }
        if (member4 != null) { member4.GroupId = g3.Id; }
        await userManager.UpdateAsync(leader!);
        if (member != null) await userManager.UpdateAsync(member);
        if (member2 != null) await userManager.UpdateAsync(member2);
        if (leader2 != null) await userManager.UpdateAsync(leader2);
        if (member3 != null) await userManager.UpdateAsync(member3);
        if (member4 != null) await userManager.UpdateAsync(member4);

        if (lecturer != null)
        {
            db.GroupLecturers.Add(new GroupLecturer { GroupId = g1.Id, LecturerUserId = lecturer.Id });
            db.GroupLecturers.Add(new GroupLecturer { GroupId = g2.Id, LecturerUserId = lecturer.Id });
        }
        if (lecturer2 != null)
        {
            db.GroupLecturers.Add(new GroupLecturer { GroupId = g2.Id, LecturerUserId = lecturer2.Id });
            db.GroupLecturers.Add(new GroupLecturer { GroupId = g3.Id, LecturerUserId = lecturer2.Id });
        }
        await db.SaveChangesAsync();

        var memberId = member!.Id;
        var member2Id = member2?.Id ?? memberId;
        var member3Id = member3?.Id ?? memberId;
        var member4Id = member4?.Id ?? memberId;

        // Bảng Tasks: 10 task
        var tasks = new List<TaskItem>
        {
            new() { Title = "Thiết kế giao diện đăng nhập", Description = "UI/UX form login", Status = TaskItemStatus.Done, JiraIssueKey = "WEB-1", AssigneeUserId = memberId, GroupId = g1.Id },
            new() { Title = "API quản lý sản phẩm", Description = "CRUD product", Status = TaskItemStatus.InProgress, JiraIssueKey = "WEB-2", AssigneeUserId = memberId, GroupId = g1.Id },
            new() { Title = "Tích hợp thanh toán", Description = "Payment gateway", Status = TaskItemStatus.Todo, JiraIssueKey = "WEB-3", AssigneeUserId = member2Id, GroupId = g1.Id },
            new() { Title = "Màn hình danh sách đơn hàng", Description = "Order list screen", Status = TaskItemStatus.Done, JiraIssueKey = "APP-1", AssigneeUserId = member3Id, GroupId = g2.Id },
            new() { Title = "Push notification", Description = "Firebase FCM", Status = TaskItemStatus.InProgress, JiraIssueKey = "APP-2", AssigneeUserId = member3Id, GroupId = g2.Id },
            new() { Title = "Load asset từ server", Description = "Game asset loader", Status = TaskItemStatus.Todo, JiraIssueKey = "GAME-1", AssigneeUserId = member4Id, GroupId = g3.Id },
            new() { Title = "Viết SRS chương 1", Description = "Introduction & overall", Status = TaskItemStatus.Done, JiraIssueKey = "SWP-1", AssigneeUserId = memberId, GroupId = g4.Id },
            new() { Title = "Setup CI/CD", Description = "GitHub Actions", Status = TaskItemStatus.InProgress, JiraIssueKey = "SWP-2", AssigneeUserId = member2Id, GroupId = g4.Id },
            new() { Title = "Unit test module Auth", Description = "Coverage > 80%", Status = TaskItemStatus.Todo, JiraIssueKey = "SWP2-1", AssigneeUserId = member3Id, GroupId = g5.Id },
            new() { Title = "Deploy staging", Description = "Deploy lên staging", Status = TaskItemStatus.Done, JiraIssueKey = "SWP2-2", AssigneeUserId = member4Id, GroupId = g5.Id },
        };
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();

        // Bảng Commits: 10 commit
        var baseTime = DateTimeOffset.UtcNow.AddDays(-14);
        var commits = new List<CommitRecord>
        {
            new() { Sha = "a1b2c3d4-g1-1", Message = "feat: add login UI", AuthorName = "SV X", AuthorEmail = "member@fpt.edu.vn", CommittedAt = baseTime.AddDays(1), UserId = memberId, GroupId = g1.Id },
            new() { Sha = "a1b2c3d4-g1-2", Message = "fix: validate form", AuthorName = "SV X", AuthorEmail = "member@fpt.edu.vn", CommittedAt = baseTime.AddDays(2), UserId = memberId, GroupId = g1.Id },
            new() { Sha = "a1b2c3d4-g1-3", Message = "feat: product API", AuthorName = "SV Y", AuthorEmail = "member2@fpt.edu.vn", CommittedAt = baseTime.AddDays(3), UserId = member2Id, GroupId = g1.Id },
            new() { Sha = "e5f6g7h8-app1", Message = "feat: order list screen", AuthorName = "SV Z", AuthorEmail = "member3@fpt.edu.vn", CommittedAt = baseTime.AddDays(4), UserId = member3Id, GroupId = g2.Id },
            new() { Sha = "e5f6g7h8-app2", Message = "chore: setup FCM", AuthorName = "SV Z", AuthorEmail = "member3@fpt.edu.vn", CommittedAt = baseTime.AddDays(5), UserId = member3Id, GroupId = g2.Id },
            new() { Sha = "i9j0k1l2-g3-1", Message = "feat: asset loader stub", AuthorName = "SV T", AuthorEmail = "member4@fpt.edu.vn", CommittedAt = baseTime.AddDays(6), UserId = member4Id, GroupId = g3.Id },
            new() { Sha = "m3n4o5p6-swp1", Message = "docs: SRS ch1", AuthorName = "SV X", AuthorEmail = "member@fpt.edu.vn", CommittedAt = baseTime.AddDays(7), UserId = memberId, GroupId = g4.Id },
            new() { Sha = "q7r8s9t0-swp2", Message = "ci: add workflow", AuthorName = "SV Y", AuthorEmail = "member2@fpt.edu.vn", CommittedAt = baseTime.AddDays(8), UserId = member2Id, GroupId = g4.Id },
            new() { Sha = "u1v2w3x4-swp2-1", Message = "test: auth service", AuthorName = "SV Z", AuthorEmail = "member3@fpt.edu.vn", CommittedAt = baseTime.AddDays(9), UserId = member3Id, GroupId = g5.Id },
            new() { Sha = "y5z6a7b8-swp2-2", Message = "deploy: staging config", AuthorName = "SV T", AuthorEmail = "member4@fpt.edu.vn", CommittedAt = baseTime.AddDays(10), UserId = member4Id, GroupId = g5.Id },
        };
        db.Commits.AddRange(commits);
        await db.SaveChangesAsync();

        // Bảng Reports: 10 báo cáo
        if (admin != null)
        {
            var reports = new List<Report>
            {
                new() { Type = ReportType.Srs, Title = "SRS - G01", Content = "Software Requirements Specification\nGroup: G01 - Nhóm 1 - Web bán hàng\n\n1. Introduction\n2. Functional Requirements\n- FR1: Đăng nhập\n- FR2: CRUD sản phẩm\n", GroupId = g1.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Progress, Title = "Báo cáo tiến độ G02", Content = "Tuần 1-2: Hoàn thành màn hình đơn hàng, đang làm push notification.", GroupId = g2.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Srs, Title = "SRS - G03", Content = "SRS Nhóm 3 - Game asset.\n1. Overview\n2. Asset pipeline.", GroupId = g3.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Progress, Title = "Tiến độ G01", Content = "Tuần 3: API product done, đang làm thanh toán.", GroupId = g1.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Srs, Title = "SRS - G04", Content = "SRS Nhóm 4 - SWP391 P01.\n1. Introduction\n2. Use cases.", GroupId = g4.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Progress, Title = "Tiến độ G04", Content = "Tuần 2: SRS chương 2 xong, CI/CD đang chạy.", GroupId = g4.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Srs, Title = "SRS - G05", Content = "SRS Nhóm 5 - SWP391 P02.\n1. Scope\n2. Requirements.", GroupId = g5.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Progress, Title = "Tiến độ G02", Content = "Tuần 4: Push notification 80% hoàn thành.", GroupId = g2.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Srs, Title = "SRS - G06", Content = "SRS Nhóm 6 - E-commerce.\n1. Overview\n2. User stories.", GroupId = g6.Id, CreatedByUserId = admin.Id },
                new() { Type = ReportType.Progress, Title = "Tiến độ G03", Content = "Tuần 1: Bắt đầu game asset loader.", GroupId = g3.Id, CreatedByUserId = admin.Id },
            };
            db.Reports.AddRange(reports);
            await db.SaveChangesAsync();
        }
    }
}

