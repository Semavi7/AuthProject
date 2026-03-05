using System.ComponentModel;

namespace AuthProject.Dtos
{
    public record ForgetPasswordRequestDto
    (
        [property: Description("Kullanıcının e-posta adresi")]
        [property: DefaultValue("ornek@email.com")]
        string? Email,

        [property: Description("Kullanıcının telefon numarası")]
        [property: DefaultValue("+905551234567")]
        string? Phone
    );
}
