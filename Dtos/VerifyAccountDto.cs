using System.ComponentModel;

namespace AuthProject.Dtos
{
    public record VerifyAccountDto
    (
        [property: Description("Kullanıcının e-posta adresi")]
        [property: DefaultValue("ornek@email.com")]
        string? Email,

        [property: Description("Kullanıcının telefon numarası")]
        [property: DefaultValue("+905551234567")]
        string? Phone,

        [property: Description("E-posta veya SMS ile gönderilen doğrulama kodu")]
        [property: DefaultValue("123456")]
        string Code
    );
}
