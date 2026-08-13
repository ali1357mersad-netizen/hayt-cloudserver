using Hayt.Data;
using Hayt.Models;
using Hayt.Licensing.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hayt.Services
{
    public static class AppSettingsService
    {
        public const string LastSelectedUserIdKey = "LastSelectedUserId";

        public static async Task<string?> GetAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            using var db = new AppDbContext();

            var settings = await db.Set<AppSetting>()
                .AsNoTracking()
                .ToListAsync();

            var item = settings.FirstOrDefault(x =>
                string.Equals(GetKeyValue(x), key, StringComparison.OrdinalIgnoreCase));

            return item == null ? null : GetSettingValue(item);
        }

        public static async Task SetAsync(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            value ??= string.Empty;

            using var db = new AppDbContext();

            var settings = await db.Set<AppSetting>().ToListAsync();

            var item = settings.FirstOrDefault(x =>
                string.Equals(GetKeyValue(x), key, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                item = new AppSetting();
                SetKeyValue(item, key);
                SetSettingValue(item, value);
                TrySetDate(item, "CreatedAt", DateTime.UtcNow);
                TrySetDate(item, "UpdatedAt", DateTime.UtcNow);
                db.Set<AppSetting>().Add(item);
            }
            else
            {
                SetSettingValue(item, value);
                TrySetDate(item, "UpdatedAt", DateTime.UtcNow);
            }

            await db.SaveChangesAsync();
        }

        public static Task<string?> GetLastSelectedUserIdAsync()
        {
            return GetAsync(LastSelectedUserIdKey);
        }

        public static Task SetLastSelectedUserIdAsync(string userId)
        {
            return SetAsync(LastSelectedUserIdKey, userId);
        }

        private static string? GetKeyValue(AppSetting item)
        {
            return Convert.ToString(GetPropertyValue(item, "Key", "SettingKey", "Name", "Code"));
        }

        private static string? GetSettingValue(AppSetting item)
        {
            return Convert.ToString(GetPropertyValue(item, "Value", "SettingValue", "Text", "Data"));
        }

        private static void SetKeyValue(AppSetting item, string value)
        {
            SetPropertyValue(item, value, "Key", "SettingKey", "Name", "Code");
        }

        private static void SetSettingValue(AppSetting item, string value)
        {
            SetPropertyValue(item, value, "Value", "SettingValue", "Text", "Data");
        }

        private static object? GetPropertyValue(object target, params string[] propertyNames)
        {
            var property = FindProperty(target.GetType(), propertyNames);
            return property?.GetValue(target);
        }

        private static void SetPropertyValue(object target, object? value, params string[] propertyNames)
        {
            var property = FindProperty(target.GetType(), propertyNames);

            if (property == null || !property.CanWrite)
            {
                throw new InvalidOperationException(
                    "مدل AppSetting باید یکی از پراپرتی‌های استاندارد Key/Value یا SettingKey/SettingValue را داشته باشد.");
            }

            if (value != null && property.PropertyType != typeof(string))
            {
                value = Convert.ChangeType(value, property.PropertyType);
            }

            property.SetValue(target, value);
        }

        private static void TrySetDate(object target, string propertyName, DateTime value)
        {
            var property = target.GetType()
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null || !property.CanWrite) return;

            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                property.SetValue(target, value);
            }
        }

        private static PropertyInfo? FindProperty(Type type, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null) return property;
            }

            return null;
        }
    }
}

