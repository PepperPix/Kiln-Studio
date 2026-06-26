using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Kiln.Studio;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new ShellWindow
            {
                DataContext = _serviceProvider.GetRequiredService<ShellViewModel>()
            };
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => _serviceProvider.GetRequiredService<IPreviewServer>().StopServer();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<EngineHost>();
        services.AddSingleton<ProjectSession>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IContentService, ContentService>();
        services.AddSingleton<IBuildService, BuildService>();
        services.AddSingleton<IDeploymentService, DeploymentService>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<IInputDialog, AvaloniaInputDialog>();
        services.AddSingleton<INewPageDialog, AvaloniaNewPageDialog>();
        services.AddSingleton<IRecentProjectsStore>(_ =>
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KilnStudio");
            Directory.CreateDirectory(baseDir);
            return new RecentProjectsStore(baseDir);
        });
        services.AddSingleton<IPreviewServer, PreviewServer>();
        services.AddSingleton<IBrowserLauncher, SystemBrowserLauncher>();
        services.AddSingleton<PreviewViewModel>();
        services.AddSingleton<ProjectExplorerViewModel>();
        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services.BuildServiceProvider();
    }
}
