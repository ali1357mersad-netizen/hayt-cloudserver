using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Windows;

namespace Hayt.Views
{
    public partial class MediaWindow
    {
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            StopAllMedia();
            Close();
        }

        private void StopAllMedia()
        {
            try
            {
                _timer.Stop();

                if (_videoUri != null)
                {
                    VideoPlayer.Stop();
                }

                if (_audioUri != null)
                {
                    AudioPlayer.Stop();
                }

                _isVideoPlaying = false;
                _isAudioPlaying = false;
            }
            catch
            {
                // هنگام بسته‌شدن پنجره نیازی به نمایش خطا نیست
            }
        }

        private static string FormatTime(TimeSpan time)
        {
            string result;

            if (time.TotalHours >= 1)
            {
                result = $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
            }
            else
            {
                result = $"{time.Minutes:00}:{time.Seconds:00}";
            }

            return ToPersianNumber(result);
        }

        private static string ToPersianNumber(object? input)
        {
            var text = input?.ToString() ?? string.Empty;

            return text
                .Replace("0", "۰")
                .Replace("1", "۱")
                .Replace("2", "۲")
                .Replace("3", "۳")
                .Replace("4", "۴")
                .Replace("5", "۵")
                .Replace("6", "۶")
                .Replace("7", "۷")
                .Replace("8", "۸")
                .Replace("9", "۹");
        }
    }}


