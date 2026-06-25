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
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<EngineHost>();
        services.AddSingleton<ProjectSession>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<ProjectExplorerViewModel>();
        services.AddSingleton<ShellViewModel>();

        return services.BuildServiceProvider();
    }
}
