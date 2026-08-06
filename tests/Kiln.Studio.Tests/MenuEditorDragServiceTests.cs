namespace Kiln.Studio.Tests;

using System.Collections.ObjectModel;
using Services;
using ViewModels;

public class MenuEditorDragServiceTests
{
    [Test]
    public async Task ComputeDropPosition_NullTarget_ReturnsAfter()
    {
        var result = MenuEditorDragService.ComputeDropPosition(null, 10, 40);

        await Assert.That(result).IsEqualTo(DropPosition.After);
    }

    [Test]
    public async Task ComputeDropPosition_ZeroHeight_ReturnsAfter()
    {
        var target = new MenuItemViewModel(new MenuItemDefinition("Target", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.ComputeDropPosition(target, 0, 0);

        await Assert.That(result).IsEqualTo(DropPosition.After);
    }

    [Test]
    public async Task ComputeDropPosition_InTopEdge_ReturnsBefore()
    {
        var target = new MenuItemViewModel(new MenuItemDefinition("Target", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.ComputeDropPosition(target, 5, 40);

        await Assert.That(result).IsEqualTo(DropPosition.Before);
    }

    [Test]
    public async Task ComputeDropPosition_InBottomEdge_ReturnsAfter()
    {
        var target = new MenuItemViewModel(new MenuItemDefinition("Target", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.ComputeDropPosition(target, 35, 40);

        await Assert.That(result).IsEqualTo(DropPosition.After);
    }

    [Test]
    public async Task ComputeDropPosition_InMiddle_ReturnsInside()
    {
        var target = new MenuItemViewModel(new MenuItemDefinition("Target", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.ComputeDropPosition(target, 20, 40);

        await Assert.That(result).IsEqualTo(DropPosition.Inside);
    }

    [Test]
    public async Task CanDrop_NullTarget_ReturnsTrue()
    {
        var dragged = new MenuItemViewModel(new MenuItemDefinition("Dragged", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.CanDrop(dragged, null, DropPosition.Before);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanDrop_TargetIsDragged_ReturnsFalse()
    {
        var dragged = new MenuItemViewModel(new MenuItemDefinition("Dragged", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.CanDrop(dragged, dragged, DropPosition.Before);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanDrop_TargetIsDescendantOfDragged_ReturnsFalse()
    {
        var parent = new MenuItemViewModel(
            new MenuItemDefinition("Parent", MenuLinkType.Ref, null, null, false,
            [
                new MenuItemDefinition("Child", MenuLinkType.Ref, null, null, false, []),
            ]),
            null);
        var child = parent.Children[0];

        var result = MenuEditorDragService.CanDrop(parent, child, DropPosition.Before);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task CanDrop_TargetIsSibling_ReturnsTrue()
    {
        var first = new MenuItemViewModel(new MenuItemDefinition("First", MenuLinkType.Ref, null, null, false, []), null);
        var second = new MenuItemViewModel(new MenuItemDefinition("Second", MenuLinkType.Ref, null, null, false, []), null);

        var result = MenuEditorDragService.CanDrop(first, second, DropPosition.Before);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task CanDrop_TargetIsGrandchildOfDragged_ReturnsFalse()
    {
        var grandparent = new MenuItemViewModel(
            new MenuItemDefinition("Grandparent", MenuLinkType.Ref, null, null, false,
            [
                new MenuItemDefinition("Parent", MenuLinkType.Ref, null, null, false,
                [
                    new MenuItemDefinition("Grandchild", MenuLinkType.Ref, null, null, false, []),
                ]),
            ]),
            null);
        var parent = grandparent.Children[0];
        var grandchild = parent.Children[0];

        var result = MenuEditorDragService.CanDrop(grandparent, grandchild, DropPosition.Before);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ResolveDropLocation_BeforeTarget_InsertsAtTargetIndex()
    {
        var first = new MenuItemViewModel(new MenuItemDefinition("First", MenuLinkType.Ref, null, null, false, []), null);
        var second = new MenuItemViewModel(new MenuItemDefinition("Second", MenuLinkType.Ref, null, null, false, []), null);
        var root = new ObservableCollection<MenuItemViewModel> { first, second };

        var (container, index) = MenuEditorDragService.ResolveDropLocation(second, first, DropPosition.Before, root);

        await Assert.That(container).IsEqualTo(root);
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveDropLocation_AfterTarget_InsertsAfterTargetIndex()
    {
        var first = new MenuItemViewModel(new MenuItemDefinition("First", MenuLinkType.Ref, null, null, false, []), null);
        var second = new MenuItemViewModel(new MenuItemDefinition("Second", MenuLinkType.Ref, null, null, false, []), null);
        var root = new ObservableCollection<MenuItemViewModel> { first, second };
        const int expectedIndex = 2;

        var (container, index) = MenuEditorDragService.ResolveDropLocation(first, second, DropPosition.After, root);

        await Assert.That(container).IsEqualTo(root);
        await Assert.That(index).IsEqualTo(expectedIndex);
    }

    [Test]
    public async Task ResolveDropLocation_InsideTarget_AppendsToTargetChildren()
    {
        var parent = new MenuItemViewModel(new MenuItemDefinition("Parent", MenuLinkType.Ref, null, null, false, []), null);
        var child = new MenuItemViewModel(new MenuItemDefinition("Child", MenuLinkType.Ref, null, null, false, []), null);
        var root = new ObservableCollection<MenuItemViewModel> { parent };

        var (container, index) = MenuEditorDragService.ResolveDropLocation(child, parent, DropPosition.Inside, root);

        await Assert.That(container).IsEqualTo(parent.Children);
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveDropLocation_NullTarget_AppendsToRoot()
    {
        var dragged = new MenuItemViewModel(new MenuItemDefinition("Dragged", MenuLinkType.Ref, null, null, false, []), null);
        var root = new ObservableCollection<MenuItemViewModel>();

        var (container, index) = MenuEditorDragService.ResolveDropLocation(dragged, null, DropPosition.After, root);

        await Assert.That(container).IsEqualTo(root);
        await Assert.That(index).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveDropLocation_BeforeNestedTarget_InsertsWithinParentChildren()
    {
        var parent = new MenuItemViewModel(new MenuItemDefinition("Parent", MenuLinkType.Ref, null, null, false, []), null);
        var firstChild = new MenuItemViewModel(new MenuItemDefinition("FirstChild", MenuLinkType.Ref, null, null, false, []), parent);
        var secondChild = new MenuItemViewModel(new MenuItemDefinition("SecondChild", MenuLinkType.Ref, null, null, false, []), parent);
        parent.Children.Add(firstChild);
        parent.Children.Add(secondChild);
        var dragged = new MenuItemViewModel(new MenuItemDefinition("Dragged", MenuLinkType.Ref, null, null, false, []), null);
        var root = new ObservableCollection<MenuItemViewModel> { parent, dragged };

        var (container, index) = MenuEditorDragService.ResolveDropLocation(dragged, secondChild, DropPosition.Before, root);

        await Assert.That(container).IsEqualTo(parent.Children);
        await Assert.That(index).IsEqualTo(1);
    }
}
