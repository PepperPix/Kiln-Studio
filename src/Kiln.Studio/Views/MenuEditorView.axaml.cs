namespace Kiln.Studio.Views;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using ViewModels;

/// <summary>
/// Code-behind for the menu editor. Handles pointer/gesture input, native
/// drag-and-drop, and visual drop-zone feedback via an adorner.
/// Domain logic lives in <see cref="MenuEditorViewModel"/> and
/// <see cref="MenuEditorDragService"/>.
/// </summary>
public partial class MenuEditorView : UserControl
{
    private MenuItemViewModel? _draggedItem;
    private TreeViewItem? _dropTarget;
    private DropPosition _dropPosition;
    private DropTargetAdorner? _adorner;
    private PointerPressedEventArgs? _dragStartEventArgs;
    private Point _dragStartPosition;
    private bool _isDragging;
    private MenuItemViewModel? _expandCandidate;
    private DateTime _expandCandidateSince;

    public MenuEditorView()
    {
        InitializeComponent();

        MenuTree.AddHandler(PointerPressedEvent, OnTreePointerPressed, handledEventsToo: true);
        MenuTree.AddHandler(PointerMovedEvent, OnTreePointerMoved, handledEventsToo: true);
        MenuTree.AddHandler(PointerReleasedEvent, OnTreePointerReleased, handledEventsToo: true);
        MenuTree.AddHandler(DragDrop.DragOverEvent, OnTreeDragOver);
        MenuTree.AddHandler(DragDrop.DropEvent, OnTreeDrop);
    }

    private void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(MenuTree).Properties.IsLeftButtonPressed)
            return;

        var item = GetItemAt(e.GetPosition(MenuTree));
        if (item is null)
            return;

        _draggedItem = item;
        _dragStartEventArgs = e;
        _dragStartPosition = e.GetPosition(this);
        _dropTarget = null;
        _isDragging = false;
    }

    private void OnTreePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedItem is null || _isDragging || _dragStartEventArgs is null)
            return;

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _dragStartPosition;

        // Only start a drag once the pointer has moved past the configured threshold.
        if (Math.Abs(delta.X) <= MenuEditorDragService.DragThreshold
            && Math.Abs(delta.Y) <= MenuEditorDragService.DragThreshold)
            return;

        _isDragging = true;
        StartDragAsync(_dragStartEventArgs);
    }

#pragma warning disable VSTHRD100 // Event handler signature requires void return.
    private async void StartDragAsync(PointerPressedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            using var data = new DataTransfer();
            data.Add(DataTransferItem.Create(DataFormat.Text, "application/x-kiln-menu-item"));

            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        }
        catch (InvalidOperationException)
        {
            // Drag start can fail if the pointer is released before the drag begins.
            // No further cleanup is needed; the finally block resets the drag state.
        }
        finally
        {
            _draggedItem = null;
            _dragStartEventArgs = null;
            _isDragging = false;
            ClearAdorner();
        }
    }

    private void OnTreePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
        {
            _draggedItem = null;
        }
    }

    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        if (_draggedItem is null || DataContext is not MenuEditorViewModel)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var position = e.GetPosition(MenuTree);
        var target = GetItemAt(position);
        _dropPosition = ComputeDropPosition(target, position);

        if (target is not null && !MenuEditorViewModel.CanDrop(_draggedItem, target, _dropPosition))
        {
            e.DragEffects = DragDropEffects.None;
            ClearAdorner();
            CancelExpandHover();
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        _dropTarget = target is null ? null : GetContainer(target);
        UpdateAdorner();
        HandleExpandOnHover(target);
    }

    private void OnTreeDrop(object? sender, DragEventArgs e)
    {
        if (_draggedItem is null || DataContext is not MenuEditorViewModel vm)
            return;

        CancelExpandHover();

        var position = e.GetPosition(MenuTree);
        var target = GetItemAt(position);
        _dropPosition = ComputeDropPosition(target, position);

        if (target is null || MenuEditorViewModel.CanDrop(_draggedItem, target, _dropPosition))
        {
            vm.Drop(_draggedItem, target, _dropPosition);
        }

        _draggedItem = null;
        _isDragging = false;
        ClearAdorner();
    }

    /// <summary>
    /// Computes the drop position relative to the target item based on the pointer
    /// position within the item's bounds.
    /// </summary>
    private MenuItemViewModel? GetItemAt(Point positionRelativeToTree)
    {
        var positionRelativeToView = positionRelativeToTree.Transform(MenuTree.TransformToVisual(this) ?? Matrix.Identity);
        var control = this.InputHitTest(positionRelativeToView) as Control;
        var container = control?.FindAncestorOfType<TreeViewItem>();
        if (container is null)
            return null;

        return container.DataContext as MenuItemViewModel;
    }

    private TreeViewItem? GetContainer(MenuItemViewModel item)
    {
        return MenuTree.ContainerFromItem(item) as TreeViewItem;
    }

    /// <summary>
    /// Computes the drop position relative to the target item based on the pointer
    /// position within the item's bounds.
    /// </summary>
    private DropPosition ComputeDropPosition(MenuItemViewModel? target, Point positionRelativeToTree)
    {
        if (target is null)
            return DropPosition.After;

        var container = GetContainer(target);
        if (container is null)
            return DropPosition.After;

        var bounds = container.Bounds;
        var origin = container.TranslatePoint(new Point(0, 0), this);
        if (!origin.HasValue)
            return DropPosition.After;

        var positionRelativeToView = positionRelativeToTree.Transform(MenuTree.TransformToVisual(this) ?? Matrix.Identity);
        var relativeY = positionRelativeToView.Y - origin.Value.Y;
        return MenuEditorDragService.ComputeDropPosition(target, relativeY, bounds.Height);
    }

    private void UpdateAdorner()
    {
        if (_dropTarget is null)
            return;

        var layer = AdornerLayer.GetAdornerLayer(MenuTree);
        if (layer is null)
            return;

        if (_adorner is null)
        {
            _adorner = new DropTargetAdorner();
            layer.Children.Add(_adorner);
            AdornerLayer.SetAdornedElement(_adorner, MenuTree);
        }

        _adorner.Update(_dropTarget, _dropPosition);
    }

    private void ClearAdorner()
    {
        if (_adorner is not null)
        {
            var layer = AdornerLayer.GetAdornerLayer(MenuTree);
            layer?.Children.Remove(_adorner);
            _adorner = null;
        }

        _dropTarget = null;
    }

    /// <summary>
    /// Expands a collapsed item with children after the pointer has hovered over it
    /// for <see cref="MenuEditorDragService.ExpandDelayMilliseconds"/>.
    /// </summary>
    /// <remarks>
    /// We poll <see cref="DateTime.UtcNow"/> instead of using a dispatcher timer
    /// because on macOS the native drag loop suspends the dispatcher timer, so the
    /// timer would never fire while a drag is in progress.
    /// </remarks>
    private void HandleExpandOnHover(MenuItemViewModel? target)
    {
        if (target is null || target.IsExpanded || target.Children.Count == 0)
        {
            CancelExpandHover();
            return;
        }

        var now = DateTime.UtcNow;
        if (target != _expandCandidate)
        {
            _expandCandidate = target;
            _expandCandidateSince = now;
            return;
        }

        var elapsed = now - _expandCandidateSince;
        if (elapsed.TotalMilliseconds >= MenuEditorDragService.ExpandDelayMilliseconds)
        {
            _expandCandidate.IsExpanded = true;
            CancelExpandHover();
        }
    }

    private void CancelExpandHover()
    {
        _expandCandidate = null;
    }
}
