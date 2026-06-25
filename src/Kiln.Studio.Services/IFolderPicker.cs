namespace Kiln.Studio.Services;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync(string title);
}
