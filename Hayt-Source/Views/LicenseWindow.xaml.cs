using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class LicenseWindow : Window
{
    public LicenseWindow()
        : this(new LicenseViewModel(new LicenseService()))
    {
    }

    public LicenseWindow(LicenseViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

