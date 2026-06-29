namespace Kiln.Studio;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

internal sealed class AvaloniaSettingsDialog : ISettingsDialog
{
    private readonly ISiteSettingsService _siteSettings;
    private readonly IDeploymentConfigStore _deploymentConfigStore;

    public AvaloniaSettingsDialog(ISiteSettingsService siteSettings, IDeploymentConfigStore deploymentConfigStore)
    {
        _siteSettings = siteSettings;
        _deploymentConfigStore = deploymentConfigStore;
    }

    public async Task ShowAsync(string projectPath)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
            return;

        var viewModel = new SettingsViewModel(_siteSettings, _deploymentConfigStore);
        viewModel.Load(projectPath);

        var window = new SettingsWindow { DataContext = viewModel };
        await window.ShowDialog(desktop.MainWindow).ConfigureAwait(true);
    }
}
