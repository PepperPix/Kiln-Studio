using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Kiln.Studio.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e) => Close();
}
