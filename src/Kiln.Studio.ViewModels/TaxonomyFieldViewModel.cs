namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;

/// <summary>
/// Bindable state for a single taxonomy (e.g. "tags", "categories") of the currently-open content
/// item: the values already committed as chips, and the autocomplete suggestions sourced from the
/// project's taxonomy value cache.
/// </summary>
public sealed class TaxonomyFieldViewModel : ViewModelBase
{
    public string Name { get; }
    public ObservableCollection<string> Values { get; } = [];
    public ObservableCollection<string> Suggestions { get; } = [];

    public TaxonomyFieldViewModel(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
