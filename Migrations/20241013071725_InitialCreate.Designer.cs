﻿// <auto-generated />
using System;
using DiaFisTransferEntegrasyonu.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiaFisTransferEntegrasyonu.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241013071725_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

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
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("email");

                    b.Property<int?>("FirmaKodu")
                        .HasColumnType("integer")
                        .HasColumnName("firma_kodu");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("full_name");

                    b.Property<bool>("IsAdmin")
                        .HasColumnType("boolean")
                        .HasColumnName("is_admin");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.ToTable("users", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
