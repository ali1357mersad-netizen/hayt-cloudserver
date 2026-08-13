using System;
using System.IO;

namespace Hayt.Services
{
    public static class MediaPathService
    {
        public static string ToAbsolutePath(string? relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(relativeOrAbsolutePath))
            {
                return relativeOrAbsolutePath;
            }

            var normalizedPath = relativeOrAbsolutePath
                .Replace("/", "\\")
                .TrimStart('\\');

            return Path.Combine(AppContext.BaseDirectory, "DataFiles", normalizedPath);
        }

        public static bool Exists(string? relativeOrAbsolutePath)
        {
            var fullPath = ToAbsolutePath(relativeOrAbsolutePath);

            return !string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath);
        }

        public static Uri? ToUriIfExists(string? relativeOrAbsolutePath)
        {
            var fullPath = ToAbsolutePath(relativeOrAbsolutePath);

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            if (!File.Exists(fullPath))
            {
                return null;
            }

            return new Uri(fullPath, UriKind.Absolute);
        }
    }
}

