# Mã Mermaid – Tất cả diagram (chỉnh trên Mermaid / mermaid.ai)

Copy từng khối code dưới đây vào [mermaid.ai](https://mermaid.ai) hoặc [mermaid.live](https://mermaid.live) để xem và chỉnh sửa. Có thể copy **cả khối** (kể cả ```mermaid và ```) hoặc **chỉ phần giữa** (từ dòng đầu flowchart/sequenceDiagram/stateDiagram-v2 đến hết).

---

## 1. Context Diagram (II.3)

```mermaid
flowchart TB
    A[Administrator]
    L[Lecturer]
    TL[Team Leader]
    TM[Team Member]
    Jira[Jira Cloud]
    GitHub[GitHub]
    S([SWP Tracker])

    A -->|Quản lý nhóm CRUD| S
    A -->|Quản lý giảng viên| S
    A -->|Gán lecturer vào nhóm| S
    A -->|Đồng bộ Jira / GitHub| S
    A -->|Tạo SRS| S
    S -->|Danh sách nhóm, báo cáo| A

    L -->|Đăng nhập| S
    L -->|Thêm/Xóa thành viên nhóm| S
    L -->|Đồng bộ GitHub| S
    S -->|Nhóm được gán, thống kê commit| L

    TL -->|Đăng nhập| S
    TL -->|Đồng bộ Jira| S
    TL -->|Tạo task, phân công member| S
    TL -->|Tạo SRS| S
    S -->|Danh sách tasks, kết quả sync, file SRS| TL

    TM -->|Đăng nhập| S
    TM -->|Cập nhật trạng thái task| S
    S -->|Task được giao, thống kê commit| TM

    Jira -->|Issues| S
    GitHub -->|Commits| S
```

---

## 2. Activity Diagram (II.5.3)

```mermaid
flowchart TB
    subgraph TeamLeader["Team Leader"]
        direction TB
        TL1([Bắt đầu])
        TL2[Chọn nhóm, Đồng bộ Jira hoặc Tạo task]
        TL3[Phân công member vào task]
    end

    subgraph System["System"]
        direction TB
        S1[Validate nhóm, Gọi Jira API]
        S2[Lưu tasks]
        S3[Lưu assignee]
        S4{Member là người được giao task?}
        S5[Cập nhật trạng thái]
        S6[Từ chối - 403]
    end

    subgraph TeamMember["Team Member"]
        direction TB
        TM1[Xem công việc được giao]
        TM2[Cập nhật trạng thái task]
        TM3([Xong])
    end

    subgraph Lecturer["Lecturer"]
        direction TB
        L1[Xem nhóm, Xem báo cáo tiến độ]
    end

    TL1 --> TL2
    TL2 --> S1
    S1 --> S2
    S2 --> TL3
    TL3 --> S3
    S3 --> TM1
    TM1 --> TM2
    TM2 --> S4
    S4 -->|Đúng| S5
    S4 -->|Sai| S6
    S5 --> TM3
    S6 --> TM3
    L1 --> S2
```

---

## 3. State Diagram – Task (III.2)

```mermaid
stateDiagram-v2
    [*] --> Todo: Tạo task mới
    Todo --> InProgress: Bắt đầu thực hiện (status = InProgress)
    InProgress --> Todo: Chuyển lại Todo (nếu cần)
    InProgress --> Done: Hoàn thành (status = Done)
    Done --> InProgress: Mở lại (nếu cần)
    Done --> [*]: (giữ Done)
```

---

## 4. Sequence Diagrams (III.1.1) – 11 UC

### 4.1 UC-01 Đăng nhập

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

### 4.2 UC-08 Cập nhật trạng thái task

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

### 4.3 UC-06 Đồng bộ Jira

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

### 4.4 UC-02 Quản lý nhóm CRUD

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

### 4.5 UC-03 Quản lý giảng viên

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

### 4.6 UC-04 Gán giảng viên vào nhóm

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

### 4.7 UC-05 Thêm/Xóa thành viên nhóm

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

### 4.8 UC-07 Quản lý công việc / Phân công task

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

### 4.9 UC-09 Tạo SRS

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

### 4.10 UC-10 Đồng bộ GitHub

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

### 4.11 UC-11 Xem thống kê commit / báo cáo

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

---

## 5. Communication Diagrams (III.1.2) – 11 UC

### 5.1 UC-01 Đăng nhập

```mermaid
flowchart LR
    V((Login View)) -->|1: POST login| A((AccountController))
    A -->|2: FindByEmail| UM((UserManager))
    UM -->|3: User| A
    A -->|4: CheckPassword| SM((SignInManager))
    SM -->|5: Result| A
    A -->|6: SignIn Cookie| SM
    A -->|7: Redirect| V
```

### 5.2 UC-02 Quản lý nhóm

```mermaid
flowchart LR
    V((Groups View)) -->|1: POST/PUT/DELETE| GC((GroupController))
    GC -->|2: Validate Code| DB((AppDbContext))
    DB -->|3: ok| GC
    GC -->|4: Save| DB
    GC -->|5: Response| V
```

### 5.3 UC-03 Quản lý giảng viên

```mermaid
flowchart LR
    V((View)) -->|1: Create/Delete| AC((AdminController))
    AC -->|2: UserManager| UM((UserManager))
    UM -->|3: DB| DB((AppDbContext))
    DB -->|4: ok| AC
    AC -->|5: Response| V
```

### 5.4 UC-04 Gán lecturer vào nhóm

```mermaid
flowchart LR
    V((View)) -->|1: POST/DELETE lecturers| AC((AdminController))
    AC -->|2: GroupLecturers Add/Remove| DB((AppDbContext))
    DB -->|3: ok| AC
    AC -->|4: 200| V
```

### 5.5 UC-05 Thêm/Xóa thành viên nhóm

```mermaid
flowchart LR
    V((View)) -->|1: GET available-users| GC((GroupController))
    GC -->|2: Query Users| DB((AppDbContext))
    DB -->|3: list| GC
    GC -->|4: POST/DELETE members| V
    V -->|5: members API| GC
    GC -->|6: Update GroupId| DB
    GC -->|7: 200| V
```

### 5.6 UC-06 Đồng bộ Jira

```mermaid
flowchart LR
    V((Sync View)) -->|1: POST sync| J((JiraController))
    J -->|2: SyncAsync| S((JiraService))
    S -->|3: GET search| API((Jira API))
    API -->|4: issues| S
    S -->|5: Upsert| D((AppDbContext))
    D -->|6: done| S
    S -->|7: (added,updated)| J
    J -->|8: JSON| V
```

### 5.7 UC-07 Quản lý công việc

```mermaid
flowchart LR
    V((Tasks View)) -->|1: POST task / PUT assign| TC((TaskController))
    TC -->|2: Load Group, Validate| DB((AppDbContext))
    DB -->|3: data| TC
    TC -->|4: Add/Update Task| DB
    TC -->|5: TaskResponse| V
```

### 5.8 UC-08 Cập nhật trạng thái task

```mermaid
flowchart LR
    subgraph " :Team Member"
        A((View))
    end
    subgraph " :API"
        B((TaskController))
    end
    subgraph " :Data"
        C((AppDbContext))
    end

    A -->|"1: PUT status"| B
    B -->|"2: Load task"| C
    C -->|"3: TaskItem"| B
    B -->|"4: Update, Save"| C
    C -->|"5: ok"| B
    B -->|"6: TaskResponse"| A
```

### 5.9 UC-09 Tạo SRS

```mermaid
flowchart LR
    V((SRS View)) -->|1: POST srs| RC((ReportController))
    RC -->|2: Load Group, Tasks| DB((AppDbContext))
    DB -->|3: data| RC
    RC -->|4: Build content, Add Report| DB
    DB -->|5: id| RC
    RC -->|6: { id, title }| V
    V -->|7: GET report download| RC
```

### 5.10 UC-10 Đồng bộ GitHub

```mermaid
flowchart LR
    V((View)) -->|1: Sync| GS((GitHubService))
    GS -->|2: GET commits| API((GitHub API))
    API -->|3: commits[]| GS
    GS -->|4: Map, Upsert Commits| DB((AppDbContext))
    DB -->|5: ok| GS
    GS -->|6: result| V
```

### 5.11 UC-11 Xem thống kê commit / báo cáo

```mermaid
flowchart LR
    V((Commits View)) -->|1: GET commit-stats, commits-by-week, progress| RC((ReportController))
    RC -->|2: Query Commits, Tasks| DB((AppDbContext))
    DB -->|3: data| RC
    RC -->|4: JSON| V
```
