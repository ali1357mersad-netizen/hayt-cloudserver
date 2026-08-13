using System;
using System.ComponentModel.DataAnnotations;

namespace Hayt.Models
{
    public class UserProgress
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string UserId { get; set; } = "default";

        public int LessonId { get; set; }

        public bool IsCompleted { get; set; }

        public int LastPosition { get; set; }

        public int Score { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Lesson? Lesson { get; set; }
    }
}
