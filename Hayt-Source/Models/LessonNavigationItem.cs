namespace Hayt.Models
{
    public class LessonNavigationItem
    {
        public int LessonId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public string DisplayTitle =>
            string.IsNullOrWhiteSpace(Title)
                ? $"درس {LessonId}"
                : Title;
    }
}