namespace Hayt.Models
{
    public class Question
    {
        public int Id { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string OptionA { get; set; } = string.Empty;

        public string OptionB { get; set; } = string.Empty;

        public string OptionC { get; set; } = string.Empty;

        public string OptionD { get; set; } = string.Empty;

        /// <summary>
        /// شماره گزینه صحیح بر اساس صفر:
        /// 0 = گزینه اول
        /// 1 = گزینه دوم
        /// 2 = گزینه سوم
        /// 3 = گزینه چهارم
        /// </summary>
        public int CorrectOptionIndex { get; set; }

        public string Explanation { get; set; } = string.Empty;

        public int OrderNumber { get; set; }

        public int LessonId { get; set; }

        public Lesson? Lesson { get; set; }

        public override string ToString()
        {
            return QuestionText;
        }
    }
}
