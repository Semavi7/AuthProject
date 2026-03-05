using System.ComponentModel;

namespace AuthProject.Dtos
{
    public record ResetPasswordDto
    (
        [property: Description("Doğrulama kaydının benzersiz kimliği")]
        Guid VerifyId,

        [property: Description("E-posta veya SMS ile gönderilen doğrulama kodu")]
        [property: DefaultValue("123456")]
        string Code,

        [property: Description("Yeni şifre (En az 6 haneli)")]
        [property: DefaultValue("newPass123")]
        string NewPassword
    );
}
