using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Model
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration _configuration;

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<UserSession> UserSessions { get; set; }

        public AuthDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configuration.GetConnectionString("AuthConnectionString");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the AuditLog table
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id); // Define the primary key
                entity.Property(a => a.UserId).IsRequired(); // Ensure UserId is required
                entity.Property(a => a.Activity).IsRequired(); // Ensure Activity is required
                entity.Property(a => a.Timestamp).HasDefaultValueSql("GETUTCDATE()"); // Set default timestamp
            });

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(us => us.Id); // Primary key
                entity.HasOne(us => us.User) // One-to-many relationship
                      .WithMany(u => u.UserSessions) // ApplicationUser has many UserSessions
                      .HasForeignKey(us => us.UserId) // Foreign key
                      .OnDelete(DeleteBehavior.Cascade); // Cascade delete
            });
        }
    }
}