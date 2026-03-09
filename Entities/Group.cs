namespace SWD.Entities;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public string? JiraProjectKey { get; set; }
    public string? GitHubRepo { get; set; }

    public List<ApplicationUser> Users { get; set; } = new();
}

