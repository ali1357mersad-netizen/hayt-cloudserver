using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services.CloudSync;

namespace Hayt.Services
{
    /// <summary>
    /// تولید گواهی پایان کتاب بدون ایجاد تغییر در ساختار دیتابیس.
    /// </summary>
    public static class CertificateService
    {
        private const string DefaultStudentName = "دانش‌پژوه اندیشکده حیات طیبه";

        /// <summary>
        /// گواهی را تولید، ذخیره و در مرورگر پیش‌فرض باز می‌کند.
        /// </summary>
        public static string GenerateAndOpen(
            BookProgressReport report,
            string? studentName = null)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            if (!report.IsCompleted ||
                report.TotalLessons <= 0 ||
                report.CompletedLessons < report.TotalLessons)
            {
                throw new InvalidOperationException(
                    "برای صدور گواهی باید تمام درس‌های کتاب کامل شده باشند.");
            }

            studentName = string.IsNullOrWhiteSpace(studentName)
                ? DefaultStudentName
                : studentName.Trim();

            var certificateCode = CreateCertificateCode(report);
            var issueDate = DateTime.Now;

            var documentsPath =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments);

            if (string.IsNullOrWhiteSpace(documentsPath))
            {
                documentsPath = AppContext.BaseDirectory;
            }

            var certificateDirectory = Path.Combine(
                documentsPath,
                "Hayt Certificates");

            Directory.CreateDirectory(certificateDirectory);

            var safeBookTitle = MakeSafeFileName(report.BookTitle);

            var fileName =
                $"Certificate-{report.BookId}-{safeBookTitle}.html";

            var filePath = Path.Combine(
                certificateDirectory,
                fileName);

            var html = BuildCertificateHtml(
                report,
                studentName,
                certificateCode,
                issueDate);

            File.WriteAllText(
                filePath,
                html,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Step20-CloudSync-Certificate-GenerateAndOpen
            _ = CloudSyncRuntimeBridge.TrackCertificateIssuedAsync(null);

            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            return filePath;
        }

        /// <summary>
        /// برای هر کتاب یک کد رهگیری ثابت و تکرارپذیر تولید می‌کند.
        /// </summary>
        public static string CreateCertificateCode(
            BookProgressReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var source =
                $"HAYT|default|{report.BookId}|{report.BookTitle}|{report.TotalLessons}";

            var sourceBytes = Encoding.UTF8.GetBytes(source);
            var hashBytes = SHA256.HashData(sourceBytes);

            var codePart = Convert
                .ToHexString(hashBytes)
                .Substring(0, 12);

            return $"HAYT-{report.BookId:D4}-{codePart}";
        }

        private static string BuildCertificateHtml(
            BookProgressReport report,
            string studentName,
            string certificateCode,
            DateTime issueDate)
        {
            var encodedStudentName =
                WebUtility.HtmlEncode(studentName);

            var encodedBookTitle =
                WebUtility.HtmlEncode(report.BookTitle);

            var encodedCertificateCode =
                WebUtility.HtmlEncode(certificateCode);

            var persianDate = GetPersianDate(issueDate);

            var averageScoreText =
                report.TotalLessons <= 0
                    ? "۰"
                    : ToPersianNumber(
                        Math.Round(
                            report.EarnedXp /
                            (double)report.TotalLessons,
                            1));

            var completedHoursText =
                ToPersianNumber(
                    Math.Round(report.CompletedHours, 1));

            var totalLessonsText =
                ToPersianNumber(report.TotalLessons);

            return $@"<!DOCTYPE html>
<html lang=""fa"" dir=""rtl"">
<head>
    <meta charset=""utf-8""/>
    <meta name=""viewport""
          content=""width=device-width, initial-scale=1""/>

    <title>گواهی پایان کتاب - {encodedBookTitle}</title>

    <style>
        * {{
            box-sizing: border-box;
        }}

        body {{
            margin: 0;
            padding: 30px;
            direction: rtl;
            color: #153536;
            background:
                radial-gradient(circle at top right,
                    rgba(46, 196, 182, 0.16),
                    transparent 35%),
                linear-gradient(135deg, #071a1b, #102f30);
            font-family:
                Tahoma,
                ""Segoe UI"",
                Arial,
                sans-serif;
        }}

        .page {{
            width: 1120px;
            min-height: 760px;
            max-width: 100%;
            margin: 0 auto;
            padding: 18px;
            background: linear-gradient(
                135deg,
                #d8b45a,
                #fff1ad,
                #b88b2d);
            box-shadow: 0 25px 80px rgba(0, 0, 0, 0.45);
        }}

        .certificate {{
            position: relative;
            min-height: 724px;
            padding: 65px 75px;
            overflow: hidden;
            text-align: center;
            background:
                linear-gradient(
                    rgba(255, 255, 255, 0.96),
                    rgba(246, 252, 249, 0.98)),
                repeating-linear-gradient(
                    45deg,
                    transparent,
                    transparent 15px,
                    rgba(15, 105, 101, 0.03) 16px);
            border: 3px solid #1c7773;
        }}

        .certificate::before,
        .certificate::after {{
            position: absolute;
            width: 230px;
            height: 230px;
            content: """";
            border: 4px double rgba(184, 139, 45, 0.55);
            transform: rotate(45deg);
        }}

        .certificate::before {{
            top: -145px;
            right: -145px;
        }}

        .certificate::after {{
            bottom: -145px;
            left: -145px;
        }}

        .organization {{
            margin-bottom: 12px;
            color: #1c7773;
            font-size: 24px;
            font-weight: 700;
        }}

        .title {{
            margin: 16px 0 8px;
            color: #b17e18;
            font-size: 52px;
            font-weight: 900;
            letter-spacing: -2px;
        }}

        .subtitle {{
            margin-bottom: 42px;
            color: #557071;
            font-size: 18px;
        }}

        .intro {{
            margin: 0;
            color: #516768;
            font-size: 19px;
        }}

        .student-name {{
            display: inline-block;
            min-width: 510px;
            margin: 20px 0;
            padding: 8px 30px 13px;
            color: #103f40;
            font-size: 35px;
            font-weight: 900;
            border-bottom: 2px solid #d1aa4e;
        }}

        .description {{
            max-width: 870px;
            margin: 5px auto 20px;
            font-size: 20px;
            line-height: 2;
        }}

        .book-title {{
            color: #126d69;
            font-size: 28px;
            font-weight: 900;
        }}

        .statistics {{
            display: flex;
            flex-wrap: wrap;
            justify-content: center;
            gap: 14px;
            margin: 35px auto;
        }}

        .stat {{
            min-width: 175px;
            padding: 15px 20px;
            background: rgba(18, 109, 105, 0.07);
            border: 1px solid rgba(18, 109, 105, 0.23);
            border-radius: 13px;
        }}

        .stat-label {{
            display: block;
            margin-bottom: 8px;
            color: #688080;
            font-size: 13px;
        }}

        .stat-value {{
            color: #174f50;
            font-size: 19px;
            font-weight: bold;
        }}

        .footer {{
            display: grid;
            grid-template-columns: 1fr 1.5fr 1fr;
            gap: 25px;
            align-items: end;
            margin-top: 60px;
        }}

        .signature {{
            padding-top: 10px;
            color: #567172;
            border-top: 1px solid #9eb0aa;
        }}

        .tracking {{
            padding: 15px;
            color: #536869;
            font-size: 13px;
            line-height: 1.9;
            background: rgba(209, 170, 78, 0.09);
            border-radius: 10px;
        }}

        .tracking-code {{
            color: #8b6214;
            direction: ltr;
            font-family: Consolas, monospace;
            font-size: 17px;
            font-weight: bold;
        }}

        .print-button {{
            display: block;
            margin: 25px auto 0;
            padding: 13px 35px;
            color: white;
            cursor: pointer;
            font-size: 16px;
            background: #126d69;
            border: 0;
            border-radius: 9px;
        }}

        @media print {{
            @page {{
                size: A4 landscape;
                margin: 0;
            }}

            body {{
                padding: 0;
                background: white;
            }}

            .page {{
                width: 100%;
                min-height: 100vh;
                padding: 12px;
                box-shadow: none;
            }}

            .certificate {{
                min-height: calc(100vh - 24px);
            }}

            .print-button {{
                display: none;
            }}
        }}
    </style>
</head>

<body>
    <div class=""page"">
        <div class=""certificate"">
            <div class=""organization"">
                اندیشکده حیات طیبه
            </div>

            <div class=""title"">
                گواهی پایان کتاب
            </div>

            <div class=""subtitle"">
                گواهی تکمیل موفق دوره آموزشی
            </div>

            <p class=""intro"">
                بدین‌وسیله گواهی می‌شود
            </p>

            <div class=""student-name"">
                {encodedStudentName}
            </div>

            <div class=""description"">
                تمام درس‌های کتاب
                <br/>

                <span class=""book-title"">
                    «{encodedBookTitle}»
                </span>

                <br/>
                را با موفقیت تکمیل کرده و شرایط دریافت
                این گواهی را به دست آورده است.
            </div>

            <div class=""statistics"">
                <div class=""stat"">
                    <span class=""stat-label"">
                        تعداد درس‌های تکمیل‌شده
                    </span>

                    <span class=""stat-value"">
                        {totalLessonsText} درس
                    </span>
                </div>

                <div class=""stat"">
                    <span class=""stat-label"">
                        درصد تکمیل
                    </span>

                    <span class=""stat-value"">
                        ۱۰۰٪
                    </span>
                </div>

                <div class=""stat"">
                    <span class=""stat-label"">
                        ساعت آموزشی
                    </span>

                    <span class=""stat-value"">
                        {completedHoursText} ساعت
                    </span>
                </div>

                <div class=""stat"">
                    <span class=""stat-label"">
                        میانگین امتیاز ثبت‌شده
                    </span>

                    <span class=""stat-value"">
                        {averageScoreText} از ۱۰۰
                    </span>
                </div>
            </div>

            <div class=""footer"">
                <div class=""signature"">
                    امضای مسئول آموزش
                </div>

                <div class=""tracking"">
                    تاریخ صدور:
                    <strong>{persianDate}</strong>
                    <br/>

                    کد رهگیری:
                    <span class=""tracking-code"">
                        {encodedCertificateCode}
                    </span>
                </div>

                <div class=""signature"">
                    مهر اندیشکده
                </div>
            </div>
        </div>
    </div>

    <button
        class=""print-button""
        onclick=""window.print()"">
        چاپ یا ذخیره به‌صورت PDF
    </button>
</body>
</html>";
        }

        private static string GetPersianDate(DateTime date)
        {
            var calendar = new PersianCalendar();

            var year = calendar.GetYear(date);
            var month = calendar.GetMonth(date);
            var day = calendar.GetDayOfMonth(date);

            return ToPersianNumber(
                $"{year:0000}/{month:00}/{day:00}");
        }

        private static string MakeSafeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Book";
            }

            var result = value.Trim();

            foreach (var invalidCharacter
                     in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(
                    invalidCharacter,
                    '-');
            }

            if (result.Length > 70)
            {
                result = result.Substring(0, 70);
            }

            return result;
        }

        private static string ToPersianNumber(object? value)
        {
            var text = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty;

            return text
                .Replace('0', '۰')
                .Replace('1', '۱')
                .Replace('2', '۲')
                .Replace('3', '۳')
                .Replace('4', '۴')
                .Replace('5', '۵')
                .Replace('6', '۶')
                .Replace('7', '۷')
                .Replace('8', '۸')
                .Replace('9', '۹')
                .Replace('.', '٫');
        }
    }
}

