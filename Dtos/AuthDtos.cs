namespace SWD.Dtos;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? UserName,
    string Role
);

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record AuthResponse(
    string AccessToken,
    string Role,
    string? Email
);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string? UserName,
    string Role
);

public sealed record SetGitHubUsernameRequest(string? GitHubUsername);

