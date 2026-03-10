namespace AuthProject.Settings
{
    public class JwtSettings
    {
        public string Secret { get; set; } = default!;
        public string RefreshSecret { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
    }
}
