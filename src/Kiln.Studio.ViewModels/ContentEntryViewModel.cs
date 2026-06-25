namespace Kiln.Studio.ViewModels;

using Kiln.Studio.Services;

public sealed class ContentEntryViewModel : ViewModelBase
{
    public string Title { get; }
    public string SourcePath { get; }
    public bool Draft { get; }
    public DateTime? Date { get; }

    public ContentEntryViewModel(ContentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Title = entry.Title;
        SourcePath = entry.SourcePath;
        Draft = entry.Draft;
        Date = entry.Date;
    }
}
