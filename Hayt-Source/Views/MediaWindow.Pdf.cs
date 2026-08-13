using Hayt.Models;
using Hayt.Licensing.Models;
using Hayt.Services;
using Hayt.Licensing.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Hayt.Views
{
    public partial class MediaWindow
    {
        private async void MediaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPdfInsideAsync();
        }

        private async Task LoadPdfInsideAsync()
        {
            if (_pdfUri == null)
            {
                PdfStatusTextBlock.Text = "برای این درس PDF پیدا نشد.";

                if (PdfWebView != null)
                {
                    PdfWebView.Visibility = Visibility.Collapsed;
                }

                if (PdfWebViewFallbackTextBlock != null)
                {
                    PdfWebViewFallbackTextBlock.Visibility = Visibility.Visible;
                    PdfWebViewFallbackTextBlock.Text = "برای این درس PDF پیدا نشد.";
                }

                return;
            }

            try
            {
                PdfStatusTextBlock.Text = "در حال آماده‌سازی نمایش داخلی PDF...";

                if (PdfWebViewFallbackTextBlock != null)
                {
                    PdfWebViewFallbackTextBlock.Visibility = Visibility.Visible;
                    PdfWebViewFallbackTextBlock.Text = "در حال بارگذاری PDF داخل برنامه...";
                }

                await PdfWebView.EnsureCoreWebView2Async();

                PdfWebView.Source = _pdfUri;
                PdfWebView.Visibility = Visibility.Visible;

                if (PdfWebViewFallbackTextBlock != null)
                {
                    PdfWebViewFallbackTextBlock.Visibility = Visibility.Collapsed;
                }

                PdfStatusTextBlock.Text = "PDF داخل برنامه بارگذاری شد.";
                FooterStatusTextBlock.Text = "PDF داخلی آماده است";
            }
            catch (Exception ex)
            {
                PdfWebView.Visibility = Visibility.Collapsed;

                if (PdfWebViewFallbackTextBlock != null)
                {
                    PdfWebViewFallbackTextBlock.Visibility = Visibility.Visible;
                    PdfWebViewFallbackTextBlock.Text =
                        "نمایش داخلی PDF انجام نشد. اگر WebView2 Runtime نصب نیست، آن را نصب کنید. خطا: " + ex.Message;
                }

                PdfStatusTextBlock.Text =
                    "نمایش داخلی PDF انجام نشد. می‌توانید PDF را با برنامه ویندوز باز کنید.";

                FooterStatusTextBlock.Text = "PDF داخلی آماده نشد";
            }
        }

        private async void ReloadPdfButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPdfInsideAsync();
        }

        private void OpenPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfUri == null)
            {
                MessageBox.Show("برای این درس PDF پیدا نشد.", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _pdfUri.LocalPath,
                    UseShellExecute = true
                });

                FooterStatusTextBlock.Text = "PDF باز شد";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا در باز کردن PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenPdfFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfUri == null)
            {
                MessageBox.Show("برای این درس PDF پیدا نشد.", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var folder = Path.GetDirectoryName(_pdfUri.LocalPath);

                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    MessageBox.Show("پوشه PDF پیدا نشد.", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });

                FooterStatusTextBlock.Text = "پوشه PDF باز شد";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطا در باز کردن پوشه PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }}


