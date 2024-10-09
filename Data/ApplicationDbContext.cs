using DiaFisTransferEntegrasyonu.Models;
using Microsoft.EntityFrameworkCore;

namespace DiaFisTransferEntegrasyonu.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Customer entity konfigürasyonu
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("customers");
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(e => e.DiaKey).HasColumnName("dia_key");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.TcNo).HasColumnName("tc_no");
                entity.Property(e => e.Vkn).HasColumnName("vkn");
            });

            // User entity konfigürasyonu
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.FullName).HasColumnName("full_name");
            });
        }
    }
}