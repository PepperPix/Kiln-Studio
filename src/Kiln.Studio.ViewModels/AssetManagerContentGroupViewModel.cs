namespace Kiln.Studio.ViewModels;

public sealed class AssetManagerContentGroupViewModel : ViewModelBase
{
    public string Title { get; }

    public AssetBrowserViewModel Browser { get; }

    public AssetManagerContentGroupViewModel(string title, AssetBrowserViewModel browser)
    {
        Title = title;
        Browser = browser;
    }
}
