namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Kiln.Studio.Services;

internal sealed class AvaloniaUnsavedChangesDialog : IUnsavedChangesDialog
{
    public async Task<UnsavedChangesDecision> ConfirmAsync(string contentName, bool allowCancel)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return UnsavedChangesDecision.Discard;

        var result = UnsavedChangesDecision.Cancel;

        var saveButton = new Button { Content = "Save", HorizontalAlignment = HorizontalAlignment.Stretch };
        var discardButton = new Button { Content = "Don't Save", HorizontalAlignment = HorizontalAlignment.Stretch };
        var cancelButton = new Button { Content = "Cancel", HorizontalAlignment = HorizontalAlignment.Stretch };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { saveButton, discardButton }
        };

        if (allowCancel)
            buttonPanel.Children.Add(cancelButton);

        var content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = $"\"{contentName}\" has unsaved changes. Save before continuing?" },
                buttonPanel
            }
        };

        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 400,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = content
        };

        saveButton.Click += (_, _) => { result = UnsavedChangesDecision.Save; dialog.Close(); };
        discardButton.Click += (_, _) => { result = UnsavedChangesDecision.Discard; dialog.Close(); };
        cancelButton.Click += (_, _) => { result = UnsavedChangesDecision.Cancel; dialog.Close(); };

        await dialog.ShowDialog(desktop.MainWindow).ConfigureAwait(true);

        return result;
    }
}
