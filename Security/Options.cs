namespace SWD.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public string SigningKey { get; init; } = default!;
    public int ExpiresMinutes { get; init; } = 120;
}

public sealed class JiraOptions
{
    public string BaseUrl { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string ApiToken { get; init; } = default!;
    public string ProjectKey { get; init; } = default!;
}

public sealed class GitHubOptions
{
    public string Owner { get; init; } = default!;
    public string Repo { get; init; } = default!;
    public string Token { get; init; } = default!;
}

