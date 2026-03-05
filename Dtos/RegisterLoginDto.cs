using System.ComponentModel;
using AuthProject.Enums;

namespace AuthProject.Dtos
{
    public record RegisterLoginDto
    (
        [property: Description("Kullanıcının e-posta adresi")]
        [property: DefaultValue("ornek@email.com")]
        string? Email,

        [property: Description("Kullanıcının telefon numarası")]
        [property: DefaultValue("+905551234567")]
        string? Phone,

        [property: Description("Kullanıcı şifresi (En az 6 haneli)")]
        [property: DefaultValue("123456")]
        string Password,

        [property: Description("Cihaz türü (Web, Ios, Android)")]
        [property: DefaultValue(DeviceType.Web)]
        DeviceType? DeviceType,

        [property: Description("Cihaz adı")]
        [property: DefaultValue("Chrome")]
        string? DeviceName,

        [property: Description("Cihazın benzersiz kimliği")]
        [property: DefaultValue("abc123-device-id")]
        string? DeviceId
    );
}
