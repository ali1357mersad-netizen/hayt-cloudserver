using System;
using System.ComponentModel.DataAnnotations;

namespace Hayt.Models
{
    public class AppUser
    {
        [Key]
        [MaxLength(200)]
        public string Id { get; set; } = "default";

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = "کاربر اصلی";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}