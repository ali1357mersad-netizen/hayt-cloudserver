using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class AISettingsWindow : Window
{
    private readonly AISettingsViewModel _viewModel;

    public AISettingsWindow()
        : this(new AISettingsViewModel(new AISettingsService()))
    {
    }

    public AISettingsWindow(AISettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        ApiKeyBox.Password = _viewModel.ApiKey ?? string.Empty;
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AISettingsViewModel viewModel)
        {
            viewModel.ApiKey = ApiKeyBox.Password;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

