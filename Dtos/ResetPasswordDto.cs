namespace AuthProject.Dtos
{
    public record ResetPasswordDto
    (
        Guid VerifyId,
        string Code,
        string NewPassword
    );
}
