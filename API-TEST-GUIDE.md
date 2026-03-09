# API Test Guide — Test All Endpoints

Base URL (when running locally): `https://localhost:7xxx` or `http://localhost:5287` (check `Properties/launchSettings.json`).

**Auth:** Use either **Cookie** (login via `/login` in browser, then call APIs from same origin with `credentials: 'include'`) or **JWT** (POST `/api/auth/login` → use `accessToken` in header: `Authorization: Bearer <token>`).

---

## 1. Auth (`/api/auth`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| POST | `/api/auth/register` | Admin (JWT/Cookie) | Register user with role | Body: `{ "email", "password", "userName", "role" }` — role: Admin, Lecturer, TeamLeader, TeamMember |
| POST | `/api/auth/login` | Anonymous | Login, returns JWT + role + email | Body: `{ "email", "password" }` → use `accessToken` for JWT calls |
| GET | `/api/auth/me` | JWT or Cookie | Current user info (id, email, userName, role) | After login; returns user + role |
| POST | `/api/auth/logout` | JWT/Cookie | Logout (client discards token) | No body; client clears token |

---

## 2. Groups (`/api/groups`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| GET | `/api/groups` | Admin, Lecturer, TeamLeader | List groups (filtered by role) | No body; Admin: all, Lecturer: assigned, Leader: own group |
| POST | `/api/groups` | Admin | Create group | Body: `{ "code", "name", "jiraProjectKey?", "githubRepo?" }` |
| PUT | `/api/groups/{id}` | Admin | Update group | Same body as create |
| DELETE | `/api/groups/{id}` | Admin | Delete group | No body |
| GET | `/api/groups/{id}/available-users` | Admin, Lecturer | Users not in this group | For “add member” dropdown |
| GET | `/api/groups/{id}/members` | Admin, Lecturer, TeamLeader | List members of group | Returns list of users in group |
| POST | `/api/groups/{id}/members` | Admin, Lecturer | Add member to group | Body: `{ "userId" }` |
| DELETE | `/api/groups/{id}/members/{userId}` | Admin, Lecturer | Remove member from group | No body |

---

## 3. Tasks (`/api/tasks`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| GET | `/api/tasks` | Any | List tasks (optional `?groupId=...`) | Query: `groupId` (optional) |
| GET | `/api/tasks/{id}` | Any | Get single task by id | Use task id from list |
| POST | `/api/tasks` | TeamLeader, Admin | Create task | Body: `{ "title", "description?", "assigneeUserId", "groupId" }` |
| PUT | `/api/tasks/{id}/status` | TeamMember, TeamLeader, Admin | Update task status | Body: `{ "status": 0|1|2 }` (Todo=0, InProgress=1, Done=2); Member only own tasks |

---

## 4. Admin (`/api/admin`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| GET | `/api/admin/lecturers` | Admin | List lecturers | No body |
| POST | `/api/admin/lecturers` | Admin | Create lecturer | Body: `{ "email", "password", "userName?" }` |
| DELETE | `/api/admin/lecturers/{id}` | Admin | Delete lecturer | No body |
| POST | `/api/admin/groups/{groupId}/lecturers/{lecturerUserId}` | Admin | Assign lecturer to group | No body |
| DELETE | `/api/admin/groups/{groupId}/lecturers/{lecturerUserId}` | Admin | Unassign lecturer from group | No body |
| GET | `/api/admin/groups/{groupId}/lecturers` | Admin | List lecturers of group | No body |
| PATCH | `/api/admin/users/{userId}/github-username` | Admin | Set user GitHub username | Body: `{ "githubUsername" }` |

---

## 5. Reports (`/api/reports`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| GET | `/api/reports/list` | Any | List reports (optional `?groupId=...`) | Query: `groupId`, `limit` (default 50) |
| GET | `/api/reports/progress` | Lecturer, TeamLeader, Admin | Progress stats for group | Query: `groupId` → total, todo, inProgress, done |
| POST | `/api/reports/srs` | TeamLeader, Admin | Generate SRS report for group | Query: `groupId` → returns report id, title, createdAt |
| GET | `/api/reports/personal-stats` | TeamMember | Current user task/commit stats | No body → totalTasks, doneTasks, inProgressTasks, totalCommits |
| GET | `/api/reports/commits` | Any | List commits | Query: `groupId` OR `userId`, `limit` (default 100) |
| GET | `/api/reports/commit-stats` | Lecturer, TeamLeader, Admin | Commit stats by user for group | Query: `groupId` → byUser, totalCommits |
| GET | `/api/reports/commits-by-week` | Any | Commits per week (chart data) | Query: `groupId` OR `userId`, `weeks` (default 8) |
| GET | `/api/reports/{id}` | Any | Get report by id | Query: `download=true` for SRS → file download |

---

## 6. Jira (`/api/jira`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| POST | `/api/jira/sync` | TeamLeader, Admin | Sync Jira project issues to tasks | Query: `groupId=...`; requires Jira config in appsettings |

---

## 7. GitHub (`/api/github`)

| Method | Endpoint | Auth | Description | How to test |
|--------|----------|------|--------------|-------------|
| POST | `/api/github/sync-commits` | Lecturer, TeamLeader, Admin | Sync repo commits for group | Query: `groupId=...`, `maxPages` (default 10); requires GitHub config |

---

## Quick test with Swagger

1. Run app: `dotnet run`
2. Open Swagger UI: `https://localhost:7xxx/swagger` (or http if no HTTPS)
3. For JWT: POST `/api/auth/login` with `{ "email": "admin@fpt.edu.vn", "password": "Admin@123" }` → copy `accessToken` → click **Authorize** → enter `Bearer <token>`.
4. For Cookie: log in at `/login` in browser, then use same origin in Swagger (or test from Dashboard pages which send cookies).

---

## Seed data (after first run)

- **Groups:** 5 (G01, G02, Echo, SWP01, SWP02)
- **Tasks:** 16 (across groups)
- **Commits:** 20 (spread across groups and weeks for charts)
- **Reports:** 6 (SRS + Progress for several groups)
- **Users:** Admin, 2 Lecturers, 2 TeamLeaders, 4 TeamMembers (see `SeedExtensions.cs` for emails/passwords).

Use group ids and user ids from GET `/api/groups` and GET `/api/admin/lecturers` (or GET `/api/auth/me`) when testing by id.
