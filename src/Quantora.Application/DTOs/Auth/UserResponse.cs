namespace Quantora.Application.DTOs.Auth;
public sealed class UserResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsEmailVerified { get; init; }
}
