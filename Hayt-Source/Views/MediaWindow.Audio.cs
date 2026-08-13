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
        private void PlayPauseAudioButton_Click(object sender, RoutedEventArgs e)
        {
            if (_audioUri == null)
            {
                MessageBox.Show("برای این درس صوت پیدا نشد.", "صوت", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isAudioPlaying)
            {
                AudioPlayer.Pause();
                _isAudioPlaying = false;
                PlayPauseAudioButton.Content = "▶ صوت";
                AudioStateTextBlock.Text = "صوت مکث شد";
                FooterStatusTextBlock.Text = "صوت در حالت مکث";
                return;
            }

            StopVideoOnly();

            AudioPlayer.SpeedRatio = _currentSpeed;
            AudioPlayer.Play();

            _isAudioPlaying = true;
            PlayPauseAudioButton.Content = "⏸ مکث";
            AudioStateTextBlock.Text = "در حال پخش صوت";
            FooterStatusTextBlock.Text = "در حال پخش صوت";
        }

        private void StopAudioButton_Click(object sender, RoutedEventArgs e)
        {
            StopAudioOnly();
        }

        private void RestartAudioButton_Click(object sender, RoutedEventArgs e)
        {
            if (_audioUri == null)
            {
                return;
            }

            StopVideoOnly();

            AudioPlayer.Position = TimeSpan.Zero;
            AudioPlayer.SpeedRatio = _currentSpeed;
            AudioPlayer.Play();

            _isAudioPlaying = true;
            PlayPauseAudioButton.Content = "⏸ مکث";
            AudioStateTextBlock.Text = "صوت از ابتدا پخش شد";
            FooterStatusTextBlock.Text = "صوت از ابتدا پخش شد";
        }

        private void StopAudioOnly()
        {
            if (_audioUri == null)
            {
                return;
            }

            AudioPlayer.Stop();
            _isAudioPlaying = false;
            PlayPauseAudioButton.Content = "▶ صوت";
            AudioStateTextBlock.Text = "صوت متوقف شد";
            AudioPositionSlider.Value = 0;
            AudioCurrentTimeTextBlock.Text = "۰۰:۰۰";
        }
    }}


