﻿// <auto-generated />
using System;
using Kattbot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Kattbot.Data.Migrations
{
    [DbContext(typeof(KattbotContext))]
    partial class KattbotContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.3")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Kattbot.Common.Models.BotRoles.BotUserRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("BotRoleType")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "BotRoleType")
                        .IsUnique();

                    b.ToTable("BotUserRoles");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Emotes.EmoteEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("EmoteAnimated")
                        .HasColumnType("boolean");

                    b.Property<decimal>("EmoteId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("EmoteName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Source")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Emotes");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("EventTemplateId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("EventTemplateId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.EventAttendee", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("EventId")
                        .HasColumnType("uuid");

                    b.Property<string>("Info")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.ToTable("EventAttendees");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.EventTemplate", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "Name")
                        .IsUnique();

                    b.ToTable("EventTemplates");
                });

            modelBuilder.Entity("Kattbot.Common.Models.GuildSetting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("GuildId", "Key")
                        .IsUnique();

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.Event", b =>
                {
                    b.HasOne("Kattbot.Common.Models.Events.EventTemplate", "EventTemplate")
                        .WithMany()
                        .HasForeignKey("EventTemplateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EventTemplate");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.EventAttendee", b =>
                {
                    b.HasOne("Kattbot.Common.Models.Events.Event", "Event")
                        .WithMany("EventAttendees")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("Kattbot.Common.Models.Events.Event", b =>
                {
                    b.Navigation("EventAttendees");
                });
#pragma warning restore 612, 618
        }
    }
}
