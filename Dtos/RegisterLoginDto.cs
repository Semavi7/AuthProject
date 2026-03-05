using AuthProject.Enums;

namespace AuthProject.Dtos
{
    public record RegisterLoginDto
    (
        string? Email,
        string? Phone,
        string Password,
        DeviceType? DeviceType,
        string? DeviceName,
        string? DeviceId
    );
}
