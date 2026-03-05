namespace AuthProject.Dtos
{
    public record ForgetPasswordRequestDto
    (
        string? Email,
        string? Phone
    );
}
