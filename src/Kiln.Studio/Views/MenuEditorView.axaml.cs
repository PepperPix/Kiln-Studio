namespace Kiln.Studio.Views;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ViewModels;

public partial class MenuEditorView : UserControl
{
    private const double DragThreshold = 5;

    private MenuItemViewModel? _draggedItem;
    private TreeViewItem? _dropTarget;
    private DropPosition _dropPosition;
    private PointerPressedEventArgs? _dragStartEventArgs;
    private Point _dragStartPosition;
    private bool _isDragging;

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
        if (Math.Abs(delta.X) <= DragThreshold && Math.Abs(delta.Y) <= DragThreshold)
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
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menu drag start failed: {ex}");
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
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        _dropTarget = target is null ? null : GetContainer(target);
        UpdateAdorner();
    }

    private void OnTreeDrop(object? sender, DragEventArgs e)
    {
        if (_draggedItem is null || DataContext is not MenuEditorViewModel vm)
            return;

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

    private MenuItemViewModel? GetItemAt(Point position)
    {
        var control = this.InputHitTest(position) as Control;
        var container = control?.FindAncestorOfType<TreeViewItem>();
        if (container is null)
            return null;

        return container.DataContext as MenuItemViewModel;
    }

    private TreeViewItem? GetContainer(MenuItemViewModel item)
    {
        return MenuTree.ContainerFromItem(item) as TreeViewItem;
    }

    private DropPosition ComputeDropPosition(MenuItemViewModel? target, Point position)
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

        var relativeY = position.Y - origin.Value.Y;
        var third = bounds.Height / 3;

        if (relativeY < third)
            return DropPosition.Before;

        if (relativeY > bounds.Height - third)
            return DropPosition.After;

        return DropPosition.Inside;
    }

    private void UpdateAdorner()
    {
        ClearAdorner();
        if (_dropTarget is null)
            return;

        _dropTarget.Classes.Add("drag-over");
    }

    private void ClearAdorner()
    {
        if (_dropTarget is not null)
        {
            _dropTarget.Classes.Remove("drag-over");
            _dropTarget = null;
        }
    }
}
