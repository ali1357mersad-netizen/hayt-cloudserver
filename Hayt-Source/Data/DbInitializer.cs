using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hayt.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync()
        {
            EnsureDataFolders();

            using var db = new AppDbContext();

            await db.Database.EnsureCreatedAsync();
            // ساخت امن جدول AppUsers در دیتابیس موجود
            await EnsureAppUsersTableAsync(db);

            // ثبت کاربر default
            await SeedDefaultUserAsync(db);

            await EnsureDefaultSettingsAsync(db);
        }

        public static void EnsureDataFolders()
        {
            var basePath = AppContext.BaseDirectory;

            CreateFolder(Path.Combine(basePath, "DataFiles"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "SeedData"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "Media"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "Media", "Videos"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "Media", "Audios"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "Media", "Pdfs"));
            CreateFolder(Path.Combine(basePath, "DataFiles", "Media", "Covers"));
        }

        private static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static async Task EnsureDefaultSettingsAsync(AppDbContext db)
        {
            var hasAnySetting = await db.AppSettings.AnyAsync();

            if (hasAnySetting)
            {
                return;
            }

            db.AppSettings.Add(new Models.AppSetting
            {
                Key = "AppName",
                Value = "اندیشکده حیات"
            });

            db.AppSettings.Add(new Models.AppSetting
            {
                Key = "AppMode",
                Value = "Offline"
            });

            db.AppSettings.Add(new Models.AppSetting
            {
                Key = "DatabaseVersion",
                Value = "1.0.0"
            });

            await db.SaveChangesAsync();
        }

        public static async Task SeedCategoriesAsync()
        {
            using var db = new AppDbContext();

            var hasAnyCategory = await db.Categories.AnyAsync();
            if (hasAnyCategory)
            {
                return;
            }

            var categoriesPath = Path.Combine(
                AppContext.BaseDirectory,
                "DataFiles",
                "SeedData",
                "categories.json");

            if (!File.Exists(categoriesPath))
            {
                Console.WriteLine("⚠️ فایل categories.json پیدا نشد: " + categoriesPath);
                return;
            }

            var json = await File.ReadAllTextAsync(categoriesPath);
            var categoriesData = System.Text.Json.JsonSerializer.Deserialize<CategoriesWrapper>(json);

            if (categoriesData?.Categories == null)
            {
                Console.WriteLine("⚠️ داده‌ای در categories.json یافت نشد");
                return;
            }

            foreach (var cat in categoriesData.Categories)
            {
                if (cat.SubCategories != null && cat.SubCategories.Count > 0)
                {
                    cat.SubCategoriesJson = System.Text.Json.JsonSerializer.Serialize(cat.SubCategories);
                }

                db.Categories.Add(cat);
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"✅ {categoriesData.Categories.Count} دسته‌بندی با موفقیت ذخیره شد");
        }

        private class CategoriesWrapper
        {
            public List<Models.Category> Categories { get; set; } = new();
        }
        private static async Task EnsureAppUsersTableAsync(AppDbContext db)
        {
            var connection = db.Database.GetDbConnection();
            try
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ""AppUsers""
                    (
                        ""Id""          TEXT NOT NULL CONSTRAINT ""PK_AppUsers"" PRIMARY KEY,
                        ""DisplayName"" TEXT NOT NULL,
                        ""CreatedAt""   TEXT NOT NULL,
                        ""LastLoginAt"" TEXT NULL,
                        ""IsActive""    INTEGER NOT NULL DEFAULT 1
                    );
                ";
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task SeedDefaultUserAsync(AppDbContext db)
        {
            var connection = db.Database.GetDbConnection();
            try
            {
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR IGNORE INTO ""AppUsers""
                        (""Id"", ""DisplayName"", ""CreatedAt"", ""IsActive"")
                    VALUES
                        ('default', 'کاربر اصلی', datetime('now'), 1);
                ";
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}

