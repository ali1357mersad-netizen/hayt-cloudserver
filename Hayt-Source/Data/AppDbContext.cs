using Hayt.Models;
using Hayt.Licensing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Hayt.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "DataFiles",
                    "Hayt.db");

                var directory = Path.GetDirectoryName(dbPath);

                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureCategory(modelBuilder);
            ConfigureUserProgress(modelBuilder);
            ConfigureAppSetting(modelBuilder);
            ConfigureAppUser(modelBuilder);
        }

        private static void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Icon)
                    .HasMaxLength(50);

                entity.Property(x => x.Color)
                    .HasMaxLength(50);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.SubCategoriesJson)
                    .HasColumnType("TEXT");
            });
        }

        private static void ConfigureUserProgress(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProgress>(entity =>
            {
                entity.ToTable("UserProgresses");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.UpdatedAt)
                    .IsRequired();

                entity.HasIndex(x => new { x.UserId, x.LessonId })
                    .IsUnique();

                entity.HasOne(x => x.Lesson)
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureAppSetting(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.ToTable("AppSettings");
                entity.HasKey(x => x.Key);

                entity.Property(x => x.Key)
                    .HasMaxLength(100);

                entity.Property(x => x.Value)
                    .HasMaxLength(500);
            });
        }
        private static void ConfigureAppUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("AppUsers");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.DisplayName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.Property(x => x.LastLoginAt);

                entity.Property(x => x.IsActive)
                    .IsRequired();
            });
        }
    }
}


