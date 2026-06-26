using Avalonia.Headless;
using Avalonia.Headless.TUnit;
using Kiln.Studio.UiTests;

[assembly: AvaloniaTestApplication(typeof(UiTestApp))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]
[assembly: AvaloniaTest]
