namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;

/// <summary>
/// Encapsulates the domain logic for drag-and-drop operations in the menu editor.
/// This service is UI-agnostic and can be unit-tested independently of Avalonia.
/// </summary>
public sealed class MenuEditorDragService
{
    /// <summary>
    /// The ratio of an item's height that is considered the "before" and "after"
    /// drop zones. The middle area is treated as "inside".
    /// </summary>
    public const double DropZoneEdgeRatio = 0.25;

    /// <summary>
    /// Delay in milliseconds before a collapsed item with children automatically
    /// expands while an item is being dragged over it.
    /// </summary>
    public const int ExpandDelayMilliseconds = 600;

    /// <summary>
    /// Minimum pointer movement in device-independent pixels before a pointer press
    /// is interpreted as a drag gesture.
    /// </summary>
    public const double DragThreshold = 5.0;

    /// <summary>
    /// Determines the drop position for a target item based on the pointer's
    /// vertical position within the item's bounds.
    /// </summary>
    /// <param name="target">The target item, or <c>null</c> to drop at the end of the root list.</param>
    /// <param name="relativeY">The pointer's Y coordinate relative to the top of the target item.</param>
    /// <param name="itemHeight">The height of the target item.</param>
    /// <returns>The computed <see cref="DropPosition"/>.</returns>
    public static DropPosition ComputeDropPosition(MenuItemViewModel? target, double relativeY, double itemHeight)
    {
        if (target is null || itemHeight <= 0)
            return DropPosition.After;

        var edge = itemHeight * DropZoneEdgeRatio;

        if (relativeY < edge)
            return DropPosition.Before;

        if (relativeY > itemHeight - edge)
            return DropPosition.After;

        return DropPosition.Inside;
    }

    /// <summary>
    /// Determines whether <paramref name="draggedItem"/> can be dropped relative to
    /// <paramref name="targetItem"/> at the given <paramref name="position"/>.
    /// </summary>
    public static bool CanDrop(MenuItemViewModel draggedItem, MenuItemViewModel? targetItem, DropPosition position)
    {
        ArgumentNullException.ThrowIfNull(draggedItem);

        if (targetItem is null)
            return true;

        if (targetItem == draggedItem)
            return false;

        if (IsDescendant(targetItem, draggedItem))
            return false;

        return true;
    }

    /// <summary>
    /// Returns the collection that should receive <paramref name="draggedItem"/>
    /// and the insertion index for the given drop operation.
    /// </summary>
    public static (ObservableCollection<MenuItemViewModel> Container, int Index) ResolveDropLocation(
        MenuItemViewModel draggedItem,
        MenuItemViewModel? targetItem,
        DropPosition position,
        ObservableCollection<MenuItemViewModel> rootItems)
    {
        ArgumentNullException.ThrowIfNull(draggedItem);
        ArgumentNullException.ThrowIfNull(rootItems);

        if (targetItem is null)
            return (rootItems, rootItems.Count);

        if (position == DropPosition.Before || position == DropPosition.After)
        {
            var container = GetContainer(targetItem, rootItems);
            var targetIndex = container.IndexOf(targetItem);
            var index = position == DropPosition.Before ? targetIndex : targetIndex + 1;
            return (container, index);
        }

        return (targetItem.Children, targetItem.Children.Count);
    }

    private static ObservableCollection<MenuItemViewModel> GetContainer(
        MenuItemViewModel item,
        ObservableCollection<MenuItemViewModel> rootItems)
    {
        return item.Parent?.Children ?? rootItems;
    }

    private static bool IsDescendant(MenuItemViewModel candidate, MenuItemViewModel ancestor)
    {
        var current = candidate.Parent;
        while (current is not null)
        {
            if (current == ancestor)
                return true;
            current = current.Parent;
        }

        return false;
    }
}
