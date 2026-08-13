using System.Collections.Generic;

namespace Hayt.Models
{
    public class BookProgressReport
    {
        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public int TotalLessons { get; set; }

        public int CompletedLessons { get; set; }

        public double CompletionPercent { get; set; }

        public double CompletedHours { get; set; }

        public int EarnedXp { get; set; }

        public bool IsCompleted { get; set; }

        public bool CanGetCertificate => IsCompleted;

        public string CertificateStatus =>
            IsCompleted ? "✓ گواهی آماده دریافت است" : "در حال آموزش";

        public string CertificateHint =>
            IsCompleted
                ? "تبریک! این کتاب کامل شده و گواهی پایان کتاب آماده دریافت است."
                : $"برای دریافت گواهی، {RemainingLessons} درس دیگر را کامل کنید.";

        public int RemainingLessons =>
            System.Math.Max(0, TotalLessons - CompletedLessons);

        public string ProgressText =>
            $"{CompletedLessons} از {TotalLessons} درس";

        public string PercentText =>
            $"{CompletionPercent:0.#}٪";

        public string HoursText =>
            $"{CompletedHours:0.#} ساعت آموزش";

        public string XpText =>
            $"{EarnedXp:N0} امتیاز";

        public string LevelTitle => GetLevelTitle(EarnedXp);

        public List<string> CompletedLessonTitles { get; set; } = new();

        public List<string> RemainingLessonTitles { get; set; } = new();

        public List<LessonNavigationItem> RemainingLessonItems { get; set; } = new();

        public string CompletedLessonsText =>
            CompletedLessonTitles.Count == 0
                ? "هنوز درسی را کامل نکرده‌اید."
                : string.Join("، ", CompletedLessonTitles);

        public string RemainingLessonsText =>
            RemainingLessonTitles.Count == 0
                ? "همه درس‌ها کامل شده است. 🎉"
                : string.Join("، ", RemainingLessonTitles);

        public string EncouragementMessage => GetEncouragementMessage();

        private string GetEncouragementMessage()
        {
            if (IsCompleted)
            {
                return "🎉 آفرین! این کتاب را با موفقیت به پایان رساندی. گواهی تو آماده است!";
            }

            if (RemainingLessons == 0)
            {
                return "🎉 همه درس‌ها کامل شده است!";
            }

            if (RemainingLessons <= 2)
            {
                return $"🔥 فقط {RemainingLessons} درس مانده! تو نزدیکی، ادامه بده!";
            }

            if (CompletionPercent >= 70)
            {
                return $"💪 عالی پیش رفتی! فقط {RemainingLessons} درس دیگر مانده، راه زیادی نمانده!";
            }

            if (CompletionPercent >= 40)
            {
                return $"🌱 خوب پیش می‌روی! {RemainingLessons} درس دیگر مانده، ادامه بده!";
            }

            if (CompletedLessons > 0)
            {
                return $"🚀 شروع خوبی داشتی! {RemainingLessons} درس دیگر مانده، ادامه بده!";
            }

            return "🌟 اولین درس را شروع کن! هر قدم تو را به گواهی نزدیک‌تر می‌کند.";
        }

        public static string GetLevelTitle(int xp)
        {
            if (xp >= 10000)
                return "مدرس حیات طیبه";

            if (xp >= 6000)
                return "استادیار";

            if (xp >= 3000)
                return "پژوهشگر";

            if (xp >= 1000)
                return "دانش‌پژوه";

            return "نوآموز";
        }
    }
}