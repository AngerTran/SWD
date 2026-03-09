namespace SWD.Entities;

/// <summary>
/// Nhiệm vụ: Admin gán Giảng viên vào nhóm (assign lecturers to groups).
/// </summary>
public class GroupLecturer
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public Guid LecturerUserId { get; set; }
    public ApplicationUser LecturerUser { get; set; } = default!;
}
