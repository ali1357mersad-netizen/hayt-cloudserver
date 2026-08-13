using System;
using System.Collections.Generic;

namespace Hayt.Models
{
    public class Book
    {
        public int Id { get; set; }

        public string BookKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CoverImagePath { get; set; } = string.Empty;

        public string Language { get; set; } = "fa";

        public int Level { get; set; } = 1;

        public string Version { get; set; } = "1.0.0";

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// شناسه دسته‌بندی کتاب
        /// مثال: education/language/english
        /// </summary>
        public string CategoryId { get; set; } = "uncategorized";

        /// <summary>
        /// عنوان دسته‌بندی برای نمایش
        /// مثال: آموزش زبان انگلیسی
        /// </summary>
        public string CategoryTitle { get; set; } = "دسته‌بندی‌نشده";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<Section> Sections { get; set; } = new();

        public override string ToString()
        {
            return Title;
        }
    }
}

