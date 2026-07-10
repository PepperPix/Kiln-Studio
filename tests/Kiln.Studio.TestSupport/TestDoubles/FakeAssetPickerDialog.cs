namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeAssetPickerDialog(AssetPickerResult? result) : IAssetPickerDialog
{
    public string? LastProjectPath { get; private set; }
    public bool? LastCanUploadToPageBundle { get; private set; }

    public Task<AssetPickerResult?> ShowAsync(string projectPath, bool canUploadToPageBundle)
    {
        LastProjectPath = projectPath;
        LastCanUploadToPageBundle = canUploadToPageBundle;
        return Task.FromResult(result);
    }
}
