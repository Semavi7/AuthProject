namespace AuthProject.Dtos
{
    public record VerifyAccountDto
    (
        string? Email,
        string? Phone,
        string Code
    );
}
