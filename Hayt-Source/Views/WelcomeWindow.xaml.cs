using System.Windows;

namespace Hayt.Views
{
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // پنجره Welcome بسته می‌شود و App.OnStartup ادامه می‌یابد
            DialogResult = true;
            Close();
        }
    }
}
