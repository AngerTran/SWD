# Checklist kiểm thử theo vai trò

Đăng nhập: http://localhost:5287/login

---

## 1. Admin (admin@fpt.edu.vn / Admin@123)

| # | Chức năng | Cách test | Kết quả mong đợi |
|---|-----------|-----------|------------------|
| 1 | Thêm nhóm | Dashboard → Quản lý nhóm → Thêm nhóm mới → Mã (VD: G10), Tên → Lưu | Lưu thành công, bảng cập nhật |
| 2 | Sửa nhóm | Bấm nút Sửa (bút chì) → sửa tên → Lưu | Cập nhật đúng |
| 3 | Xóa nhóm | Bấm Xóa (thùng rác) → Xác nhận | Nhóm biến mất khỏi bảng |
| 4 | Thêm giảng viên | Dashboard → Giảng viên → Thêm giảng viên → Email, Username, Mật khẩu → Tạo | Xuất hiện trong danh sách |
| 5 | Xóa giảng viên | Bấm Xóa bên cạnh 1 GV | GV biến mất |
| 6 | Thêm thành viên vào nhóm | Quản lý nhóm → nút Thêm thành viên (user+) trên 1 nhóm → Chọn user → Thêm | Số thành viên tăng, user đã chọn nằm trong nhóm |

---

## 2. Lecturer (lecturer@fpt.edu.vn / Lecturer@123)

| # | Chức năng | Cách test | Kết quả mong đợi |
|---|-----------|-----------|------------------|
| 1 | Xem danh sách nhóm | Dashboard → Danh sách nhóm | Chỉ thấy nhóm được gán (G01, G02 nếu seed) |
| 2 | Thêm thành viên vào nhóm | Trong Danh sách nhóm → nút Thêm thành viên (user+) → Chọn user chưa ở trong nhóm → Thêm | Thêm thành công, cột Thành viên cập nhật |
| 3 | Không thêm/sửa/xóa nhóm | Trang Danh sách nhóm | Không có nút "Thêm nhóm mới", không có nút Sửa/Xóa nhóm |
| 4 | Requirements | Dashboard → Requirements → Chọn nhóm → Xem tasks | Hiển thị danh sách công việc theo nhóm |
| 5 | Báo cáo | Dashboard → Báo cáo | Gọi API progress / commit-stats theo nhóm (nếu có trang) |

---

## 3. Team Leader (leader@fpt.edu.vn / Leader@123)

| # | Chức năng | Cách test | Kết quả mong đợi |
|---|-----------|-----------|------------------|
| 1 | Xem nhóm | Dashboard → Quản lý công việc (Tasks) hoặc Danh sách nhóm | Chỉ thấy nhóm của mình (G01 nếu seed) |
| 2 | Thêm task | Công việc → Thêm thủ công → Tiêu đề, Mô tả, Chọn Người thực hiện (dropdown) → Lưu | Task mới xuất hiện trong bảng |
| 3 | Đồng bộ Jira | Công việc → Đồng bộ Jira | Gọi API sync (thành công hoặc báo cấu hình Jira) |
| 4 | Tạo SRS | Dashboard → Tạo SRS (nếu có link) hoặc gọi API POST /api/reports/srs?groupId=... | Báo cáo SRS được tạo |
| 5 | Xem progress | Trang Công việc | Số Tổng / Đã hoàn thành cập nhật đúng |

---

## 4. Team Member (member@fpt.edu.vn / Member@123)

| # | Chức năng | Cách test | Kết quả mong đợi |
|---|-----------|-----------|------------------|
| 1 | Xem công việc của tôi | Dashboard → Công việc của tôi | Chỉ thấy task được giao cho mình |
| 2 | Cập nhật trạng thái task | Trong bảng, đổi dropdown trạng thái (To Do / In Progress / Done) | Lưu thành công, danh sách refresh |
| 3 | Thống kê cá nhân | Cột bên phải | Số Hoàn thành, Đang làm, Tổng, Commits hiển thị đúng |
| 4 | Không thêm task/nhóm | Không vào được Tasks (quản lý) hay Quản lý nhóm (CRUD) | Menu chỉ có Công việc của tôi, Commits |

---

## 5. Kiểm tra chung

| # | Nội dung | Cách test |
|---|----------|-----------|
| 1 | Cookie auth | Đăng nhập → mở tab mới cùng localhost → vào Dashboard → vẫn đăng nhập |
| 2 | Đăng xuất | Bấm Đăng xuất ở sidebar | Về trang /login, vào lại Dashboard bị chuyển về login |
| 3 | Mã nhóm trùng | Admin thêm nhóm với Mã đã có (VD: G01) | Báo lỗi: "Mã nhóm đã tồn tại" |
| 4 | Add member – user đã trong nhóm | Lecturer thêm thành viên: chọn user đã ở nhóm đó | API có thể báo lỗi hoặc không hiện trong "available-users" (user đã trong nhóm không nằm trong danh sách chọn) |

---

Chạy app: `dotnet run`  
Database đã có seed: 5 nhóm, users (Admin, 2 Lecturer, 2 Leader, 4 Member), tasks, commits. Dùng các tài khoản trên để test.
