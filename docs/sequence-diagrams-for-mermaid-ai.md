# Sequence Diagrams – Code cho mermaid.ai

Copy từng khối code dưới đây (không lấy dòng ```mermaid và ```) và dán vào [mermaid.ai](https://mermaid.ai) để xem hình. Hoặc copy nguyên cả khối kể cả ```mermaid và ``` nếu mermaid.ai chấp nhận.

---

## 1. UC-01 Đăng nhập

```mermaid
sequenceDiagram
    participant U as User
    participant V as Login View
    participant A as AccountController
    participant UM as UserManager
    participant SM as SignInManager

    U->>V: Nhập email, password
    V->>A: POST /login (email, password)
    A->>UM: FindByEmailAsync(email)
    UM-->>A: ApplicationUser
    A->>SM: CheckPasswordSignInAsync(user, password)
    SM-->>A: SignInResult
    A->>A: Build Claims (NameIdentifier, Role, Email)
    A->>SM: SignInAsync(Cookie, principal)
    A-->>V: Redirect /Dashboard/Index
    V-->>U: Chuyển theo role (Admin/Lecturer/TeamLeader/TeamMember)
```

---

## 2. UC-08 Cập nhật trạng thái task

```mermaid
sequenceDiagram
    participant M as Team Member
    participant V as TeamMember View
    participant TC as TaskController
    participant DB as AppDbContext

    M->>V: Chọn trạng thái (Todo/Working/Done)
    V->>TC: PUT /api/tasks/{id}/status { status }
    TC->>DB: FirstOrDefaultAsync(task)
    DB-->>TC: TaskItem
    TC->>TC: Check: Member chỉ sửa task được giao cho mình
    alt AssigneeUserId != currentUser
        TC-->>V: 403 Forbid
    else OK
        TC->>DB: task.Status = req.Status; SaveChangesAsync()
        DB-->>TC: saved
        TC-->>V: 200 TaskResponse
    end
    V-->>M: Cập nhật badge & thống kê
```

---

## 3. UC-06 Đồng bộ Jira

```mermaid
sequenceDiagram
    participant TL as Team Leader
    participant V as Sync View
    participant JC as JiraController
    participant JS as JiraService
    participant API as Jira Cloud API
    participant DB as AppDbContext

    TL->>V: Chọn nhóm, bấm "Đồng bộ ngay"
    V->>JC: POST /api/jira/sync?groupId=...
    JC->>DB: Groups.FirstOrDefaultAsync(groupId)
    DB-->>JC: Group
    JC->>JS: SyncProjectIssuesToTasksAsync(groupId, userId)
    JS->>API: GET /rest/api/3/search?jql=project=KEY
    API-->>JS: issues[]
    JS->>JS: Map issue → TaskItem (key, summary, status, assignee)
    JS->>DB: Upsert Tasks (Find + Add/Update), SaveChangesAsync
    DB-->>JS: done
    JS-->>JC: (added, updated)
    JC-->>V: 200 { added, updated }
    V-->>TL: Hiển thị kết quả đồng bộ
```

---

## 4. UC-02 Quản lý nhóm CRUD

```mermaid
sequenceDiagram
    participant A as Administrator
    participant V as Groups View
    participant GC as GroupController
    participant DB as AppDbContext

    A->>V: Thêm/Sửa/Xóa nhóm
    V->>GC: POST/PUT/DELETE /api/groups (Code, Name, JiraProjectKey, GitHubRepo)
    GC->>DB: Kiểm tra Code trùng, Add/Update/Remove
    DB-->>GC: saved
    GC-->>V: 200 GroupResponse / 400 duplicate
    V-->>A: Cập nhật danh sách nhóm
```

---

## 5. UC-03 Quản lý giảng viên

```mermaid
sequenceDiagram
    participant A as Administrator
    participant V as Lecturers View
    participant AC as AdminController
    participant UM as UserManager
    participant DB as AppDbContext

    A->>V: Tạo/Xóa lecturer
    V->>AC: POST/DELETE admin API (email, role Lecturer)
    AC->>UM: CreateAsync/DeleteAsync(user)
    UM->>DB: Lưu AspNetUsers, AspNetUserRoles
    DB-->>AC: ok
    AC-->>V: 200 / error
    V-->>A: Cập nhật danh sách
```

---

## 6. UC-04 Gán giảng viên vào nhóm

```mermaid
sequenceDiagram
    participant A as Administrator
    participant V as View
    participant AC as AdminController
    participant DB as AppDbContext

    A->>V: Chọn nhóm, lecturer, bấm Gán/Bỏ gán
    V->>AC: POST/DELETE /api/admin/groups/{id}/lecturers
    AC->>DB: GroupLecturers.Add/Remove, SaveChanges
    DB-->>AC: ok
    AC-->>V: 200
    V-->>A: Cập nhật danh sách lecturer của nhóm
```

---

## 7. UC-05 Thêm/Xóa thành viên nhóm

```mermaid
sequenceDiagram
    participant U as Admin/Lecturer
    participant V as Groups View
    participant GC as GroupController
    participant DB as AppDbContext

    U->>V: Chọn nhóm, Thêm/Xóa thành viên
    V->>GC: GET /api/groups/{id}/available-users
    GC->>DB: Users không thuộc nhóm / Members của nhóm
    DB-->>GC: list
    GC-->>V: available-users / members
    V->>GC: POST/DELETE /api/groups/{id}/members { userId }
    GC->>DB: Update user.GroupId, SaveChanges
    DB-->>GC: ok
    GC-->>V: 200
    V-->>U: Cập nhật danh sách thành viên
```

---

## 8. UC-07 Quản lý công việc / Phân công task

```mermaid
sequenceDiagram
    participant TL as Team Leader/Admin
    participant V as Tasks View
    participant TC as TaskController
    participant DB as AppDbContext

    TL->>V: Tạo task hoặc Phân công member
    V->>TC: POST /api/tasks hoặc PUT /api/tasks/{id}/assign
    TC->>DB: Tasks.Add / task.AssigneeUserId = x, SaveChanges
    DB-->>TC: TaskItem
    TC-->>V: 200 TaskResponse
    V-->>TL: Cập nhật bảng tasks
```

---

## 9. UC-09 Tạo SRS

```mermaid
sequenceDiagram
    participant U as Team Leader/Admin
    participant V as SRS View
    participant RC as ReportController
    participant DB as AppDbContext

    U->>V: Chọn nhóm, bấm Tạo SRS
    V->>RC: POST /api/reports/srs?groupId=...
    RC->>DB: Load Group, Tasks (Include AssigneeUser)
    DB-->>RC: data
    RC->>RC: Build SRS content, new Report
    RC->>DB: Reports.Add, SaveChanges
    DB-->>RC: report.Id
    RC-->>V: 200 { id, title }
    V-->>U: Hiển thị link tải GET /api/reports/{id}?download=true
```

---

## 10. UC-10 Đồng bộ GitHub

```mermaid
sequenceDiagram
    participant U as Lecturer/Leader/Admin
    participant V as View
    participant API as GitHub API
    participant GS as GitHubService
    participant DB as AppDbContext

    U->>V: Chọn nhóm, đồng bộ GitHub
    V->>GS: Sync (groupId, repo)
    GS->>API: GET /repos/{owner}/{repo}/commits
    API-->>GS: commits[]
    GS->>GS: Map author → User, upsert CommitRecord
    GS->>DB: Commits Add/Update, SaveChanges
    DB-->>GS: done
    GS-->>V: (added, updated)
    V-->>U: Hiển thị kết quả
```

---

## 11. UC-11 Xem thống kê commit / báo cáo

```mermaid
sequenceDiagram
    participant U as User (any role)
    participant V as Commits/Reports View
    participant RC as ReportController
    participant DB as AppDbContext

    U->>V: Vào trang Commits / chọn nhóm
    V->>RC: GET /api/reports/commit-stats, commits-by-week, progress, personal-stats
    RC->>DB: Query Commits, Tasks theo groupId/assigneeUserId
    DB-->>RC: data
    RC-->>V: JSON (counts, weekly data, list)
    V-->>U: Hiển thị biểu đồ, bảng commit, thống kê
```
