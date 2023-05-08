﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusForever.Database.Auth;

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20200426162617_AccountItems")]
    partial class AccountItems
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountCostumeUnlockModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("ItemId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("itemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<DateTime>("Timestamp")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                b.HasKey("Id", "ItemId")
                    .HasName("PRIMARY");

                b.ToTable("account_costume_unlock");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountCurrencyModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<byte>("CurrencyId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("currencyId")
                    .HasColumnType("tinyint(4) unsigned")
                    .HasDefaultValue((byte)0);

                b.Property<ulong>("Amount")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("amount")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                b.HasKey("Id", "CurrencyId")
                    .HasName("PRIMARY");

                b.ToTable("account_currency");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountEntitlementModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<byte>("EntitlementId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("entitlementId")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                b.Property<uint>("Amount")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("amount")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.HasKey("Id", "EntitlementId")
                    .HasName("PRIMARY");

                b.ToTable("account_entitlement");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountGenericUnlockModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("Entry")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("entry")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<DateTime>("Timestamp")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                b.HasKey("Id", "Entry")
                    .HasName("PRIMARY");

                b.ToTable("account_generic_unlock");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountItemCooldownModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("CooldownGroupId")
                    .HasColumnName("cooldownGroupId")
                    .HasColumnType("int(10) unsigned");

                b.Property<uint>("Duration")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("duration")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<DateTime?>("Timestamp")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("timestamp")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                b.HasKey("Id", "CooldownGroupId")
                    .HasName("PRIMARY");

                b.ToTable("account_item_cooldown");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountItemModel", b =>
            {
                b.Property<ulong>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("entry")
                    .HasColumnType("bigint(20) unsigned")
                    .HasDefaultValue(0ul);

                b.Property<uint>("AccountId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("accountId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("ItemId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("itemId")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.HasKey("Id", "AccountId")
                    .HasName("PRIMARY");

                b.HasIndex("AccountId");

                b.ToTable("account_item");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountKeybindingModel", b =>
            {
                b.Property<uint>("Id")
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<ushort>("InputActionId")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("inputActionId")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue((ushort)0);

                b.Property<uint>("Code00")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("code00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("Code01")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("code01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("Code02")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("code02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("DeviceEnum00")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("deviceEnum00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("DeviceEnum01")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("deviceEnum01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("DeviceEnum02")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("deviceEnum02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("EventTypeEnum00")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("eventTypeEnum00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("EventTypeEnum01")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("eventTypeEnum01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("EventTypeEnum02")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("eventTypeEnum02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("MetaKeys00")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("metaKeys00")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("MetaKeys01")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("metaKeys01")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.Property<uint>("MetaKeys02")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("metaKeys02")
                    .HasColumnType("int(10) unsigned")
                    .HasDefaultValue(0u);

                b.HasKey("Id", "InputActionId")
                    .HasName("PRIMARY");

                b.ToTable("account_keybinding");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountModel", b =>
            {
                b.Property<uint>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id")
                    .HasColumnType("int(10) unsigned");

                b.Property<DateTime>("CreateTime")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("createTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("current_timestamp()");

                b.Property<string>("Email")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("email")
                    .HasColumnType("varchar(128)")
                    .HasDefaultValue("");

                b.Property<string>("GameToken")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("gameToken")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                b.Property<string>("S")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("s")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                b.Property<string>("SessionKey")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("sessionKey")
                    .HasColumnType("varchar(32)")
                    .HasDefaultValue("");

                b.Property<string>("V")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("v")
                    .HasColumnType("varchar(512)")
                    .HasDefaultValue("");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .HasName("email");

                b.HasIndex("GameToken")
                    .HasName("gameToken");

                b.HasIndex("SessionKey")
                    .HasName("sessionKey");

                b.ToTable("account");
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.ServerMessageModel", b =>
            {
                b.Property<byte>("Index")
                    .HasColumnName("index")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                b.Property<byte>("Language")
                    .HasColumnName("language")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                b.Property<string>("Message")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("message")
                    .HasColumnType("varchar(256)")
                    .HasDefaultValue("");

                b.HasKey("Index", "Language")
                    .HasName("PRIMARY");

                b.ToTable("server_message");

                b.HasData(
                    new
                    {
                        Index = (byte)0,
                        Language = (byte)0,
                        Message = @"Welcome to this NexusForever server!
Visit: https://github.com/NexusForever/NexusForever"
                    },
                    new
                    {
                        Index = (byte)0,
                        Language = (byte)1,
                        Message = @"Willkommen auf diesem NexusForever server!
Besuch: https://github.com/NexusForever/NexusForever"
                    });
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.ServerModel", b =>
            {
                b.Property<byte>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id")
                    .HasColumnType("tinyint(3) unsigned");

                b.Property<string>("Host")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("host")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("127.0.0.1");

                b.Property<string>("Name")
                    .IsRequired()
                    .ValueGeneratedOnAdd()
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasDefaultValue("NexusForever");

                b.Property<ushort>("Port")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("port")
                    .HasColumnType("smallint(5) unsigned")
                    .HasDefaultValue((ushort)24000);

                b.Property<byte>("Type")
                    .ValueGeneratedOnAdd()
                    .HasColumnName("type")
                    .HasColumnType("tinyint(3) unsigned")
                    .HasDefaultValue((byte)0);

                b.HasKey("Id");

                b.ToTable("server");

                b.HasData(
                    new
                    {
                        Id = (byte)1,
                        Host = "127.0.0.1",
                        Name = "NexusForever",
                        Port = (ushort)24000,
                        Type = (byte)0
                    });
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountCostumeUnlockModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountCostumeUnlock")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_costume_item_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountCurrencyModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountCurrency")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_currency_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountEntitlementModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountEntitlement")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_entitlement_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountGenericUnlockModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountGenericUnlock")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_generic_unlock_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountItemCooldownModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountItemCooldown")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_item_cooldown_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountItemModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountItem")
                    .HasForeignKey("AccountId")
                    .HasConstraintName("FK__account_item_accountId__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("NexusForever.Database.Auth.Model.AccountKeybindingModel", b =>
            {
                b.HasOne("NexusForever.Database.Auth.Model.AccountModel", "Account")
                    .WithMany("AccountKeybinding")
                    .HasForeignKey("Id")
                    .HasConstraintName("FK__account_keybinding_id__account_id")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
#pragma warning restore 612, 618
        }
    }
}
