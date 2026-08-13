using System.Collections.Generic;

namespace Hayt.Models
{
    public class Chapter
    {
        public int Id { get; set; }

        public string ChapterKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int SectionId { get; set; }

        public Section? Section { get; set; }

        public List<Lesson> Lessons { get; set; } = new();

        public override string ToString()
        {
            return Title;
        }
    }
}
