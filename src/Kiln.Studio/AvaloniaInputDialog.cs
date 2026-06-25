namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Kiln.Studio.Services;

internal sealed class AvaloniaInputDialog : IInputDialog
{
    public async Task<string?> PromptAsync(string title, string message)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return null;

        var textBox = new TextBox { MinWidth = 300 };
        var okButton = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Stretch };
        var cancelButton = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Stretch };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { okButton, cancelButton }
        };

        var content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = message },
                textBox,
                buttonPanel
            }
        };

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = content
        };

        string? result = null;

        okButton.Click += (_, _) =>
        {
            result = textBox.Text;
            dialog.Close();
        };

        cancelButton.Click += (_, _) => dialog.Close();

        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                result = textBox.Text;
                dialog.Close();
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
            }
        };

        await dialog.ShowDialog(desktop.MainWindow).ConfigureAwait(true);

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}
