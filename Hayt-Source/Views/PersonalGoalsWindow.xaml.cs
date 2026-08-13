using System.Windows;
using Hayt.Services;
using Hayt.Licensing.Services;
using Hayt.ViewModels;

namespace Hayt.Views;

public partial class PersonalGoalsWindow : Window
{
    public PersonalGoalsWindow()
        : this(new PersonalGoalsViewModel(new PersonalGoalsService()))
    {
    }

    public PersonalGoalsWindow(PersonalGoalsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

