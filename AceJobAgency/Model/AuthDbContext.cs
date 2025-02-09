using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Model
{
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration _configuration;

        // Add DbSet for AuditLog
        public DbSet<AuditLog> AuditLogs { get; set; }

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
        }
    }
}