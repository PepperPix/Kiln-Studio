namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;

internal static class ObservableCollectionExtensions
{
    /// <summary>
    /// Returns the item and all descendants in a depth-first order.
    /// </summary>
    public static IEnumerable<MenuItemViewModel> DescendantsAndSelf(this ObservableCollection<MenuItemViewModel> source)
    {
        foreach (var item in source)
        {
            yield return item;
            foreach (var descendant in item.Children.DescendantsAndSelf())
                yield return descendant;
        }
    }

    /// <summary>
    /// Returns the item and all descendants in a depth-first order.
    /// </summary>
    public static IEnumerable<MenuItemViewModel> DescendantsAndSelf(this MenuItemViewModel source)
    {
        yield return source;
        foreach (var descendant in source.Children.DescendantsAndSelf())
            yield return descendant;
    }
}
