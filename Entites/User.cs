using AuthProject.Enums;
using Microsoft.AspNetCore.Identity;

namespace AuthProject.Entites
{
    public class User : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        public Guid? PhoneVerifyId { get; set; }
        public Guid? EmailVerifyId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();
        public ICollection<Socialite> Socialites { get; set; } = new List<Socialite>();
        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }
}
