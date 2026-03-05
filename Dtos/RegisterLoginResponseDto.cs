using AuthProject.Enums;

namespace AuthProject.Dtos
{
    public record RegisterLoginResponseDto
    (
        Guid Id,
        string? Email,
        string? Phone,
        string? FirstName,
        string? LastName,
        UserStatus Status
    );
}
