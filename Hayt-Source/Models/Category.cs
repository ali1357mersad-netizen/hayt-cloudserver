using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hayt.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// زیردسته‌ها به صورت JSON در دیتابیس ذخیره می‌شود.
        /// </summary>
        public string? SubCategoriesJson { get; set; }

        /// <summary>
        /// زیردسته‌ها برای استفاده در کد؛ در دیتابیس ذخیره نمی‌شود.
        /// </summary>
        [NotMapped]
        public List<SubCategoryItem>? SubCategories { get; set; }
    }

    public class SubCategoryItem
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public List<SubCategoryItem>? SubCategories { get; set; }
    }
}
