namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

internal sealed class AvaloniaSettingsDialog : ISettingsDialog
{
    private readonly ISiteSettingsService _siteSettings;

    public AvaloniaSettingsDialog(ISiteSettingsService siteSettings)
    {
        _siteSettings = siteSettings;
    }

    public async Task ShowAsync(string projectPath)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return;

        var viewModel = new SettingsViewModel(_siteSettings);
        viewModel.Load(projectPath);

        var window = new SettingsWindow { DataContext = viewModel };
        await window.ShowDialog(desktop.MainWindow).ConfigureAwait(true);
    }
}
