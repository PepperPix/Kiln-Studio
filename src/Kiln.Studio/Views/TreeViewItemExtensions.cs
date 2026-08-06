namespace Kiln.Studio.Views;

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

/// <summary>
/// Shared helpers for locating parts within a <see cref="TreeViewItem"/>'s visual tree.
/// </summary>
internal static class TreeViewItemExtensions
{
    /// <summary>
    /// Finds the <c>PART_HeaderPresenter</c> that renders <paramref name="container"/>'s header.
    /// </summary>
    public static ContentPresenter? GetHeaderPresenter(this TreeViewItem container)
    {
        return container.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .FirstOrDefault(p => p.Name == "PART_HeaderPresenter");
    }
}
