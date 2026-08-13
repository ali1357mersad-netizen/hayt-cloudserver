using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Windows;
using System.Windows.Media;

namespace Hayt.Views
{
    public partial class MediaWindow
    {
        private void PlayPauseVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoUri == null)
            {
                MessageBox.Show("برای این درس ویدئو پیدا نشد.", "ویدئو", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isVideoPlaying)
            {
                VideoPlayer.Pause();
                _isVideoPlaying = false;
                PlayPauseVideoButton.Content = "▶ پخش";
                VideoStateTextBlock.Text = "ویدئو مکث شد";
                FooterStatusTextBlock.Text = "ویدئو در حالت مکث";
                return;
            }

            StopAudioOnly();

            VideoPlayer.SpeedRatio = _currentSpeed;
            VideoPlayer.Play();

            _isVideoPlaying = true;
            PlayPauseVideoButton.Content = "⏸ مکث";
            VideoStateTextBlock.Text = "در حال پخش ویدئو";
            FooterStatusTextBlock.Text = "در حال پخش ویدئو";
        }

        private void StopVideoButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideoOnly();
        }

        private void RestartVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoUri == null)
            {
                return;
            }

            StopAudioOnly();

            VideoPlayer.Position = TimeSpan.Zero;
            VideoPlayer.SpeedRatio = _currentSpeed;
            VideoPlayer.Play();

            _isVideoPlaying = true;
            PlayPauseVideoButton.Content = "⏸ مکث";
            VideoStateTextBlock.Text = "ویدئو از ابتدا پخش شد";
            FooterStatusTextBlock.Text = "ویدئو از ابتدا پخش شد";
        }

        private void BackVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoUri == null)
            {
                return;
            }

            var newPosition = VideoPlayer.Position - TimeSpan.FromSeconds(10);

            if (newPosition < TimeSpan.Zero)
            {
                newPosition = TimeSpan.Zero;
            }

            VideoPlayer.Position = newPosition;
            UpdateVideoPositionUi();
        }

        private void ForwardVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoUri == null)
            {
                return;
            }

            var duration = GetVideoDuration();
            var newPosition = VideoPlayer.Position + TimeSpan.FromSeconds(10);

            if (duration.HasValue && newPosition > duration.Value)
            {
                newPosition = duration.Value;
            }

            VideoPlayer.Position = newPosition;
            UpdateVideoPositionUi();
        }

        private void StopVideoOnly()
        {
            if (_videoUri == null)
            {
                return;
            }

            VideoPlayer.Stop();
            _isVideoPlaying = false;
            PlayPauseVideoButton.Content = "▶ پخش";
            VideoStateTextBlock.Text = "ویدئو متوقف شد";
            PositionSlider.Value = 0;
            CurrentTimeTextBlock.Text = "۰۰:۰۰";
        }
    }}


