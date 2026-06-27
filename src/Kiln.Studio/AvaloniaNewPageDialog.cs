namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Kiln.Studio.Services;

internal sealed class AvaloniaNewPageDialog : INewPageDialog
{
    public async Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return null;

        var collectionBox = new ComboBox
        {
            ItemsSource = collectionNames,
            SelectedIndex = 0,
            MinWidth = 200
        };

        var titleBox = new TextBox { MinWidth = 200, PlaceholderText = "Page title" };

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
                new TextBlock { Text = "Collection:" },
                collectionBox,
                new TextBlock { Text = "Title:" },
                titleBox,
                buttonPanel
            }
        };

        var dialog = new Window
        {
            Title = "New Page",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = content
        };

        NewPageRequest? result = null;

        okButton.Click += (_, _) =>
        {
            var collection = collectionBox.SelectedItem?.ToString();
            var title = titleBox.Text;
            if (!string.IsNullOrWhiteSpace(collection) && !string.IsNullOrWhiteSpace(title))
            {
                result = new NewPageRequest(collection, title);
                dialog.Close();
            }
        };

        cancelButton.Click += (_, _) => dialog.Close();

        titleBox.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                var collection = collectionBox.SelectedItem?.ToString();
                var title = titleBox.Text;
                if (!string.IsNullOrWhiteSpace(collection) && !string.IsNullOrWhiteSpace(title))
                {
                    result = new NewPageRequest(collection, title);
                    dialog.Close();
                }
            }
            else if (e.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
            }
        };

        await dialog.ShowDialog(desktop.MainWindow).ConfigureAwait(true);
        return result;
    }
}
