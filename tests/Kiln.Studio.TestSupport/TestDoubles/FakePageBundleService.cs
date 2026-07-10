namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakePageBundleService : IPageBundleService
{
    public bool IsPageBundleResult { get; set; }
    public PageBundleUploadResult? UploadAssetResult { get; set; }
    public string? LastSourcePath { get; private set; }
    public string? LastUploadedFilePath { get; private set; }

    public bool IsPageBundle(string sourcePath)
    {
        LastSourcePath = sourcePath;
        return IsPageBundleResult;
    }

    public PageBundleUploadResult UploadAsset(string sourcePath, string uploadedFilePath)
    {
        LastSourcePath = sourcePath;
        LastUploadedFilePath = uploadedFilePath;
        return UploadAssetResult ?? new PageBundleUploadResult(sourcePath, Path.GetFileName(uploadedFilePath), WasConverted: false);
    }
}
