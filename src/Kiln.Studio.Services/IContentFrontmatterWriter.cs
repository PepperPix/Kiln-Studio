namespace Kiln.Studio.Services;

public interface IContentFrontmatterWriter
{
    bool SetDraft(string sourcePath, bool draft);

    bool ToggleDraft(string sourcePath);
}
