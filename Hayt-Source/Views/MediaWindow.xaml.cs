using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hayt.Views
{
    public partial class MediaWindow : Window
    {
        private readonly Lesson _lesson;
        private readonly Uri? _videoUri;
        private readonly Uri? _audioUri;
        private readonly Uri? _pdfUri;

        private readonly DispatcherTimer _timer;

        private bool _isVideoPlaying;
        private bool _isAudioPlaying;
        private bool _isVideoSliderDragging;
        private bool _isAudioSliderDragging;
        private bool _videoMediaOpened;
        private bool _audioMediaOpened;

        private double _currentSpeed = 1.0;

        public MediaWindow(Lesson lesson)
        {
            InitializeComponent();

            _lesson = lesson ?? throw new ArgumentNullException(nameof(lesson));

            _videoUri = MediaPathService.ToUriIfExists(_lesson.VideoPath);
            _audioUri = MediaPathService.ToUriIfExists(_lesson.AudioPath);
            _pdfUri = MediaPathService.ToUriIfExists(_lesson.PdfPath);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _timer.Tick += Timer_Tick;

            InitializeUi();
            InitializeMedia();

            Loaded += MediaWindow_Loaded;

            _timer.Start();
        }

        private void InitializeUi()
        {
            Title = "رسانه: " + _lesson.Title;
            TitleTextBlock.Text = "رسانه: " + _lesson.Title;

            var videoStatus = _videoUri != null ? "ویدئو موجود" : "ویدئو ندارد";
            var audioStatus = _audioUri != null ? "صوت موجود" : "صوت ندارد";
            var pdfStatus = _pdfUri != null ? "PDF موجود" : "PDF ندارد";

            StatusTextBlock.Text = videoStatus + " | " + audioStatus + " | " + pdfStatus;

            VideoBadgeText.Text = _videoUri != null ? "ویدئو ✓" : "ویدئو ×";
            AudioBadgeText.Text = _audioUri != null ? "صوت ✓" : "صوت ×";
            PdfBadgeText.Text = _pdfUri != null ? "PDF ✓" : "PDF ×";

            SetBadgeState(VideoBadge, _videoUri != null);
            SetBadgeState(AudioBadge, _audioUri != null);
            SetBadgeState(PdfBadge, _pdfUri != null);

            VideoEmptyTextBlock.Visibility = _videoUri == null
                ? Visibility.Visible
                : Visibility.Collapsed;

            VideoStateTextBlock.Text = _videoUri != null
                ? "ویدئو آماده پخش است"
                : "ویدئو برای این درس پیدا نشد";

            AudioStateTextBlock.Text = _audioUri != null
                ? "صوت آماده پخش است"
                : "صوت برای این درس پیدا نشد";

            AudioTitleTextBlock.Text = _audioUri != null
                ? Path.GetFileNameWithoutExtension(_audioUri.LocalPath)
                : "فایل صوتی پیدا نشد";

            PdfStatusTextBlock.Text = _pdfUri != null
                ? "PDF برای این درس موجود است. فعلاً با برنامه پیش‌فرض ویندوز باز می‌شود."
                : "برای این درس PDF پیدا نشد.";

            OpenPdfButton.IsEnabled = _pdfUri != null;
            OpenPdfFolderButton.IsEnabled = _pdfUri != null;

            PlayPauseVideoButton.IsEnabled = _videoUri != null;
            StopVideoButton.IsEnabled = _videoUri != null;
            RestartVideoButton.IsEnabled = _videoUri != null;
            BackVideoButton.IsEnabled = _videoUri != null;
            ForwardVideoButton.IsEnabled = _videoUri != null;
            PositionSlider.IsEnabled = _videoUri != null;

            PlayPauseAudioButton.IsEnabled = _audioUri != null;
            StopAudioButton.IsEnabled = _audioUri != null;
            RestartAudioButton.IsEnabled = _audioUri != null;
            AudioPositionSlider.IsEnabled = _audioUri != null;

            VolumeSlider.Value = 0.75;

            FooterStatusTextBlock.Text = "آماده";
        }

        private void InitializeMedia()
        {
            if (_videoUri != null)
            {
                VideoPlayer.Source = _videoUri;
                VideoPlayer.Volume = VolumeSlider.Value;
                VideoPlayer.SpeedRatio = GetInitialSpeed();
            }

            if (_audioUri != null)
            {
                AudioPlayer.Source = _audioUri;
                AudioPlayer.Volume = VolumeSlider.Value;
                AudioPlayer.SpeedRatio = GetInitialSpeed();
            }

            _currentSpeed = GetInitialSpeed();
        }

        private double GetInitialSpeed()
        {
            if (_lesson.DefaultPlaybackSpeed <= 0)
            {
                return 1.0;
            }

            if (_lesson.DefaultPlaybackSpeed < 0.5)
            {
                return 0.5;
            }

            if (_lesson.DefaultPlaybackSpeed > 2.0)
            {
                return 2.0;
            }

            return _lesson.DefaultPlaybackSpeed;
        }

        private static void SetBadgeState(
            System.Windows.Controls.Border badge,
            bool isAvailable)
        {
            if (badge == null)
            {
                return;
            }

            badge.Background = isAvailable
                ? new SolidColorBrush(
                    Color.FromRgb(46, 160, 67))
                : new SolidColorBrush(
                    Color.FromRgb(218, 54, 51));
        }
    }
}


