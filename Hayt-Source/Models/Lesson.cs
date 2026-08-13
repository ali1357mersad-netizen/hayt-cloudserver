using System.Collections.Generic;

namespace Hayt.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        public string LessonKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string VideoPath { get; set; } = string.Empty;

        public string AudioPath { get; set; } = string.Empty;

        public string PdfPath { get; set; } = string.Empty;

        public int Level { get; set; } = 1;

        public int OrderNumber { get; set; }

        public int EstimatedMinutes { get; set; } = 10;

        public string LessonType { get; set; } = "Educational";

        public bool IsActive { get; set; } = true;

        public string Tags { get; set; } = string.Empty;

        public int PassingScore { get; set; } = 70;

        public bool AllowDownload { get; set; } = true;

        public bool AllowShare { get; set; } = false;

        public double DefaultPlaybackSpeed { get; set; } = 1.0;

        public int ChapterId { get; set; }

        public Chapter? Chapter { get; set; }

        public List<Question> Questions { get; set; } = new();

        public override string ToString()
        {
            return Title;
        }
    }
}
