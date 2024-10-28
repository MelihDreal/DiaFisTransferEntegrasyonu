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
        // Add the new DbSet properties here
        public DbSet<Cari> Cariler { get; set; }
        public DbSet<BankaHesabi> BankaHesaplari { get; set; }
        public DbSet<KasaKarti> KasaKartlari { get; set; }
        public DbSet<OdemePlani> OdemePlanlari { get; set; }

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
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.ApiKey).HasColumnName("api_key");
                entity.Property(e => e.ApiUrl).HasColumnName("api_url");
                entity.Property(e => e.SubeKodu).HasColumnName("SubeKodu");
                entity.Property(e => e.FirmaKodu).HasColumnName("firma_kodu");
                entity.Property(e => e.DonemKodu).HasColumnName("donem_kodu");
                entity.Property(e => e.IsAdmin).HasColumnName("is_admin");
                entity.Property(e => e.UstIslemTuru).HasColumnName("ust_islem_turu");

                // Relationships
                entity.HasMany(u => u.KasaKartlari).WithOne(k => k.User).HasForeignKey(k => k.UserId);
                entity.HasMany(u => u.OdemePlanlari).WithOne(o => o.User).HasForeignKey(o => o.UserId);
                entity.HasMany(u => u.BankaHesaplari).WithOne(b => b.User).HasForeignKey(b => b.UserId);
                entity.HasMany(u => u.Cariler).WithOne(c => c.User).HasForeignKey(c => c.UserId);
            });

            modelBuilder.Entity<Cari>(entity =>
            {
                entity.ToTable("Cari");
                entity.Property(e => e.Id).HasColumnName("Id").UseIdentityAlwaysColumn();
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.CariId).HasColumnName("CariId");
                entity.Property(e => e.CariAdi).HasColumnName("CariAdi");

            });

            modelBuilder.Entity<BankaHesabi>(entity =>
            {
                entity.ToTable("BankaHesabi");
                entity.Property(e => e.Id).HasColumnName("Id").UseIdentityAlwaysColumn();
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.HesapId).HasColumnName("HesapId");
                entity.Property(e => e.HesapAdi).HasColumnName("HesapAdi");

            });

            modelBuilder.Entity<OdemePlani>(entity =>
            {
                entity.ToTable("OdemePlani");
                entity.Property(e => e.Id).HasColumnName("Id").UseIdentityAlwaysColumn();
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.PlanId).HasColumnName("PlanId");
                entity.Property(e => e.PlanAdi).HasColumnName("PlanAdi");
                entity.Property(e => e.PlanHesapKey).HasColumnName("PlanHesapKey");

            });

            modelBuilder.Entity<KasaKarti>(entity =>
            {
                entity.ToTable("KasaKarti");
                entity.Property(e => e.Id).HasColumnName("Id").UseIdentityAlwaysColumn();
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.KasaId).HasColumnName("KasaId");
                entity.Property(e => e.KasaAdi).HasColumnName("KasaAdi");

            });
        }
    }
}