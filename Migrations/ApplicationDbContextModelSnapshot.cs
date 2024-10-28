﻿// <auto-generated />
using System;
using DiaFisTransferEntegrasyonu.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiaFisTransferEntegrasyonu.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.BankaHesabi", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("Id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("HesapAdi")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("HesapAdi");

                    b.Property<string>("HesapId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("HesapId");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("BankaHesabi", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.Cari", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("Id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("CariAdi")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("CariAdi");

                    b.Property<string>("CariId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("CariId");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Cari", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.Customer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .HasColumnType("text")
                        .HasColumnName("code");

                    b.Property<int>("DiaKey")
                        .HasColumnType("integer")
                        .HasColumnName("dia_key");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("TcNo")
                        .HasColumnType("text")
                        .HasColumnName("tc_no");

                    b.Property<string>("Vkn")
                        .HasColumnType("text")
                        .HasColumnName("vkn");

                    b.HasKey("Id");

                    b.ToTable("customers", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.KasaKarti", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("Id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("KasaAdi")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("KasaAdi");

                    b.Property<string>("KasaId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("KasaId");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("KasaKarti", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.OdemePlani", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("Id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("PlanAdi")
                        .HasColumnType("text")
                        .HasColumnName("PlanAdi");

                    b.Property<string>("PlanHesapKey")
                        .HasColumnType("text")
                        .HasColumnName("PlanHesapKey");

                    b.Property<string>("PlanId")
                        .HasColumnType("text")
                        .HasColumnName("PlanId");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("OdemePlani", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityAlwaysColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("api_key");

                    b.Property<string>("ApiUrl")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("api_url");

                    b.Property<int?>("DonemKodu")
                        .HasColumnType("integer")
                        .HasColumnName("donem_kodu");

                    b.Property<string>("Email")
                        .HasColumnType("text")
                        .HasColumnName("email");

                    b.Property<int?>("FirmaKodu")
                        .HasColumnType("integer")
                        .HasColumnName("firma_kodu");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("boolean")
                        .HasColumnName("is_admin");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password");

                    b.Property<DateTime>("SonGuncellenmeTarihi")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SubeKodu")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("SubeKodu");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.Property<int?>("UstIslemTuru")
                        .HasColumnType("integer")
                        .HasColumnName("ust_islem_turu");

                    b.HasKey("Id");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.BankaHesabi", b =>
                {
                    b.HasOne("DiaFisTransferEntegrasyonu.Models.User", "User")
                        .WithMany("BankaHesaplari")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.Cari", b =>
                {
                    b.HasOne("DiaFisTransferEntegrasyonu.Models.User", "User")
                        .WithMany("Cariler")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.KasaKarti", b =>
                {
                    b.HasOne("DiaFisTransferEntegrasyonu.Models.User", "User")
                        .WithMany("KasaKartlari")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.OdemePlani", b =>
                {
                    b.HasOne("DiaFisTransferEntegrasyonu.Models.User", "User")
                        .WithMany("OdemePlanlari")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DiaFisTransferEntegrasyonu.Models.User", b =>
                {
                    b.Navigation("BankaHesaplari");

                    b.Navigation("Cariler");

                    b.Navigation("KasaKartlari");

                    b.Navigation("OdemePlanlari");
                });
#pragma warning restore 612, 618
        }
    }
}
