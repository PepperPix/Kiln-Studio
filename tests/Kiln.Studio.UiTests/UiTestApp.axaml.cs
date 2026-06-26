using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;

namespace Kiln.Studio.UiTests;

internal sealed class UiTestApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<UiTestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseSkia();
}
