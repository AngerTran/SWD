using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SWD.Entities;

namespace SWD.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupLecturer> GroupLecturers => Set<GroupLecturer>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<CommitRecord> Commits => Set<CommitRecord>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Group>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(x => x.Group)
                .WithMany(g => g.Users)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GroupLecturer>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.LecturerUserId });
            e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LecturerUser).WithMany().HasForeignKey(x => x.LecturerUserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskItem>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.JiraIssueKey).HasMaxLength(32);
            e.HasIndex(x => x.JiraIssueKey);

            e.HasOne(x => x.AssigneeUser)
                .WithMany()
                .HasForeignKey(x => x.AssigneeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CommitRecord>(e =>
        {
            e.Property(x => x.Sha).HasMaxLength(64).IsRequired();
            e.HasIndex(x => new { x.GroupId, x.Sha }).IsUnique().HasFilter("[GroupId] IS NOT NULL");
            e.HasIndex(x => x.Sha).IsUnique().HasFilter("[GroupId] IS NULL");

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Report>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

