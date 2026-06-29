namespace Kiln.Studio.Services;

public enum DraftFilter { All, PublishedOnly, DraftsOnly }

public enum ContentSortMode { Default, DateNewest, DateOldest, TitleAscending, TitleDescending }

public static class ContentQuery
{
    public static IReadOnlyList<ContentEntry> Apply(
        IReadOnlyList<ContentEntry> entries,
        string? searchText,
        DraftFilter draftFilter,
        ContentSortMode sortMode)
    {
        var result = entries.AsEnumerable();

        result = draftFilter switch
        {
            DraftFilter.PublishedOnly => result.Where(e => !e.Draft),
            DraftFilter.DraftsOnly => result.Where(e => e.Draft),
            _ => result
        };

        if (!string.IsNullOrEmpty(searchText))
        {
            var search = searchText;
            result = result.Where(e =>
                e.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(e.SourcePath).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = result.ToList();
        switch (sortMode)
        {
            case ContentSortMode.Default:
                break;
            case ContentSortMode.DateNewest:
                list = list.OrderByDescending(e => e.Date.HasValue)
                           .ThenByDescending(e => e.Date)
                           .ToList();
                break;
            case ContentSortMode.DateOldest:
                list = list.OrderByDescending(e => e.Date.HasValue)
                           .ThenBy(e => e.Date)
                           .ToList();
                break;
            case ContentSortMode.TitleAscending:
                list = list.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList();
                break;
            case ContentSortMode.TitleDescending:
                list = list.OrderByDescending(e => e.Title, StringComparer.OrdinalIgnoreCase).ToList();
                break;
        }

        return list;
    }
}
