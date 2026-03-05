using System.ComponentModel;
using AuthProject.Enums;

namespace AuthProject.Dtos
{
    public record RegisterLoginResponseDto
    (
        [property: Description("Kullanıcının benzersiz kimliği")]
        Guid Id,

        [property: Description("Kullanıcının e-posta adresi")]
        [property: DefaultValue("ornek@email.com")]
        string? Email,

        [property: Description("Kullanıcının telefon numarası")]
        [property: DefaultValue("+905551234567")]
        string? Phone,

        [property: Description("Kullanıcının adı")]
        [property: DefaultValue("Ahmet")]
        string? FirstName,

        [property: Description("Kullanıcının soyadı")]
        [property: DefaultValue("Yılmaz")]
        string? LastName,

        [property: Description("Kullanıcının hesap durumu")]
        [property: DefaultValue(UserStatus.Active)]
        UserStatus Status
    );
}
