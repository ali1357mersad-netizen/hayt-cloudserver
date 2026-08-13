using System.Collections.Generic;

namespace Hayt.Models
{
    public class Section
    {
        public int Id { get; set; }

        public string SectionKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int BookId { get; set; }

        public Book? Book { get; set; }

        public List<Chapter> Chapters { get; set; } = new();

        public override string ToString()
        {
            return Title;
        }
    }
}
