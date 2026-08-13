using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Hayt.Views
{
    public partial class MediaWindow
    {
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_isVideoSliderDragging)
            {
                UpdateVideoPositionUi();
            }

            if (!_isAudioSliderDragging)
            {
                UpdateAudioPositionUi();
            }
        }

        private void UpdateVideoPositionUi()
        {
            if (!_videoMediaOpened)
            {
                return;
            }

            var duration = GetVideoDuration();

            if (!duration.HasValue || duration.Value.TotalSeconds <= 0)
            {
                return;
            }

            PositionSlider.Maximum = duration.Value.TotalSeconds;
            PositionSlider.Value = Math.Min(VideoPlayer.Position.TotalSeconds, PositionSlider.Maximum);

            CurrentTimeTextBlock.Text = FormatTime(VideoPlayer.Position);
            DurationTimeTextBlock.Text = FormatTime(duration.Value);
        }

        private void UpdateAudioPositionUi()
        {
            if (!_audioMediaOpened)
            {
                return;
            }

            var duration = GetAudioDuration();

            if (!duration.HasValue || duration.Value.TotalSeconds <= 0)
            {
                return;
            }

            AudioPositionSlider.Maximum = duration.Value.TotalSeconds;
            AudioPositionSlider.Value = Math.Min(AudioPlayer.Position.TotalSeconds, AudioPositionSlider.Maximum);

            AudioCurrentTimeTextBlock.Text = FormatTime(AudioPlayer.Position);
            AudioDurationTimeTextBlock.Text = FormatTime(duration.Value);
        }

        private TimeSpan? GetVideoDuration()
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                return VideoPlayer.NaturalDuration.TimeSpan;
            }

            return null;
        }

        private TimeSpan? GetAudioDuration()
        {
            if (AudioPlayer.NaturalDuration.HasTimeSpan)
            {
                return AudioPlayer.NaturalDuration.TimeSpan;
            }

            return null;
        }

        private void PositionSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isVideoSliderDragging = true;
        }

        private void PositionSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_videoMediaOpened)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(PositionSlider.Value);
            }

            _isVideoSliderDragging = false;
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isVideoSliderDragging)
            {
                CurrentTimeTextBlock.Text = FormatTime(TimeSpan.FromSeconds(PositionSlider.Value));
            }
        }

        private void AudioPositionSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isAudioSliderDragging = true;
        }

        private void AudioPositionSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_audioMediaOpened)
            {
                AudioPlayer.Position = TimeSpan.FromSeconds(AudioPositionSlider.Value);
            }

            _isAudioSliderDragging = false;
        }

        private void AudioPositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isAudioSliderDragging)
            {
                AudioCurrentTimeTextBlock.Text = FormatTime(TimeSpan.FromSeconds(AudioPositionSlider.Value));
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoPlayer != null)
            {
                VideoPlayer.Volume = VolumeSlider.Value;
            }

            if (AudioPlayer != null)
            {
                AudioPlayer.Volume = VolumeSlider.Value;
            }
        }

        private void SetSpeed(double speed)
        {
            _currentSpeed = speed;

            if (VideoPlayer != null)
            {
                VideoPlayer.SpeedRatio = speed;
            }

            if (AudioPlayer != null)
            {
                AudioPlayer.SpeedRatio = speed;
            }

            FooterStatusTextBlock.Text = "سرعت پخش: " + speed.ToString("0.##") + "x";
        }

        private void Speed05Button_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed(0.5);
        }

        private void Speed1Button_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed(1.0);
        }

        private void Speed125Button_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed(1.25);
        }

        private void Speed15Button_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed(1.5);
        }

        private void Speed2Button_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed(2.0);
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            _videoMediaOpened = true;

            var duration = GetVideoDuration();

            if (duration.HasValue)
            {
                PositionSlider.Maximum = duration.Value.TotalSeconds;
                DurationTimeTextBlock.Text = FormatTime(duration.Value);
            }

            VideoPlayer.Volume = VolumeSlider.Value;
            VideoPlayer.SpeedRatio = _currentSpeed;
            VideoStateTextBlock.Text = "ویدئو آماده پخش است";
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _isVideoPlaying = false;
            PlayPauseVideoButton.Content = "▶ پخش";
            VideoStateTextBlock.Text = "پخش ویدئو تمام شد";
            FooterStatusTextBlock.Text = "پخش ویدئو تمام شد";
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _isVideoPlaying = false;
            PlayPauseVideoButton.Content = "▶ پخش";
            VideoStateTextBlock.Text = "خطا در پخش ویدئو";

            MessageBox.Show(
                e.ErrorException?.Message ?? "خطای نامشخص در پخش ویدئو رخ داد.",
                "خطا در پخش ویدئو",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            _audioMediaOpened = true;

            var duration = GetAudioDuration();

            if (duration.HasValue)
            {
                AudioPositionSlider.Maximum = duration.Value.TotalSeconds;
                AudioDurationTimeTextBlock.Text = FormatTime(duration.Value);
            }

            AudioPlayer.Volume = VolumeSlider.Value;
            AudioPlayer.SpeedRatio = _currentSpeed;
            AudioStateTextBlock.Text = "صوت آماده پخش است";
        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _isAudioPlaying = false;
            PlayPauseAudioButton.Content = "▶ صوت";
            AudioStateTextBlock.Text = "پخش صوت تمام شد";
            FooterStatusTextBlock.Text = "پخش صوت تمام شد";
        }

        private void AudioPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _isAudioPlaying = false;
            PlayPauseAudioButton.Content = "▶ صوت";
            AudioStateTextBlock.Text = "خطا در پخش صوت";

            MessageBox.Show(
                e.ErrorException?.Message ?? "خطای نامشخص در پخش صوت رخ داد.",
                "خطا در پخش صوت",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }}


