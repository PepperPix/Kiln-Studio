namespace Kiln.Studio.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using ViewModels;

public partial class MenuEditorView : UserControl
{
    private MenuItemViewModel? _draggedItem;
    private TreeViewItem? _dropTarget;
    private DropPosition _dropPosition;

    public MenuEditorView()
    {
        InitializeComponent();

        MenuTree.AddHandler(PointerPressedEvent, OnTreePointerPressed, handledEventsToo: true);
        MenuTree.AddHandler(DragDrop.DragOverEvent, OnTreeDragOver);
        MenuTree.AddHandler(DragDrop.DropEvent, OnTreeDrop);
    }

#pragma warning disable VSTHRD100 // Event handler signature requires void return.
    private async void OnTreePointerPressed(object? sender, PointerPressedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            if (!e.GetCurrentPoint(MenuTree).Properties.IsLeftButtonPressed)
                return;

            var item = GetItemAt(e.GetPosition(MenuTree));
            if (item is null)
                return;

            _draggedItem = item;
            _dropTarget = null;

            using var data = new DataTransfer();
            var format = DataFormat.CreateInProcessFormat<MenuItemViewModel>("application/x-kiln-menu-item");
            data.Add(DataTransferItem.Create(format, _draggedItem));

            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move).ConfigureAwait(true);
        }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menu drag start failed: {ex}");
        }
        finally
        {
            _draggedItem = null;
            ClearAdorner();
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
