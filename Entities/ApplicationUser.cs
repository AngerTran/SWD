using Microsoft.AspNetCore.Identity;

namespace SWD.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    /// <summary>GitHub login/username để map commit author với user trong hệ thống.</summary>
    public string? GitHubUsername { get; set; }
}

