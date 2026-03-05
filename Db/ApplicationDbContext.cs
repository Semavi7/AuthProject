using AuthProject.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthProject.Db
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Verify> Verifies { get; set; }
        public DbSet<ForgetPassword> ForgetPasswords { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Socialite> Socialites { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            builder.Entity<User>().ToTable("users");
            builder.Entity<IdentityRole<Guid>>().ToTable("roles");

            builder.Entity<Socialite>()
                .HasIndex(s => new { s.Type, s.RefId })
                .IsUnique();
        }
    }
}
