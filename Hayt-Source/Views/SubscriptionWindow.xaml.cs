using System;
using System.Windows;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class SubscriptionWindow : Window
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionWindow()
    {
        InitializeComponent();

        _viewModel = new SubscriptionViewModel();
        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

    public SubscriptionWindow(
        SubscriptionViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ??
            throw new ArgumentNullException(nameof(viewModel));

        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }
}