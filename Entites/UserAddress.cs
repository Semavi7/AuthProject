using AuthProject.Enums;

namespace AuthProject.Entites
{
    public class UserAddress
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public AddressType Type { get; set; }
        public bool IsDefault { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public Guid CityId { get; set; }
        public Guid NeighBourhood { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string? ZipCode { get; set; }
        public bool IsCorporate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string TaxOffice { get; set; } = string.Empty;

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdateAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeleteAt { get; set; }

        public User User { get; set; }
    }
}
