namespace Kiln.Studio.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using ViewModels;

/// <summary>
/// Adorner that renders drop-zone feedback for a <see cref="TreeViewItem"/>:
/// a line at the top (Before), a line at the bottom (After), or a filled
/// rectangle with a border (Inside).
/// </summary>
#pragma warning disable CA1501 // Avalonia Control hierarchy is inherently deep; inheritance is required.
internal sealed class DropTargetAdorner : Control
#pragma warning restore CA1501
{
    private static readonly IBrush LineBrush = new SolidColorBrush(Colors.DodgerBlue);
    private static readonly IBrush InsideBrush = new SolidColorBrush(Colors.DodgerBlue, 0.2);

    private TreeViewItem? _target;
    private DropPosition _position;

    public DropTargetAdorner()
    {
        // The adorner must not intercept pointer or drag/drop events; it is only
        // visual feedback. Events should pass through to the adorned TreeView.
        IsHitTestVisible = false;
    }

    /// <summary>
    /// Updates the adorner to target a new tree item and/or position and invalidates
    /// the visual if anything changed.
    /// </summary>
    public void Update(TreeViewItem target, DropPosition position)
    {
        var needsInvalidate = !ReferenceEquals(_target, target) || _position != position;
        _target = target;
        _position = position;

        if (needsInvalidate)
        {
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var adorned = AdornerLayer.GetAdornedElement(this);
        if (adorned is null || _target is null)
            return;

        var header = (Control?)GetHeaderPresenter(_target) ?? _target;
        var targetBounds = header.Bounds;
        var topLeft = header.TranslatePoint(new Point(0, 0), adorned);
        if (!topLeft.HasValue)
            return;

        var rect = new Rect(topLeft.Value.X, topLeft.Value.Y, targetBounds.Width, targetBounds.Height);
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        const double thickness = 2.0;

        switch (_position)
        {
            case DropPosition.Before:
                context.FillRectangle(LineBrush, new Rect(rect.X, rect.Y, rect.Width, thickness));
                break;

            case DropPosition.After:
                context.FillRectangle(LineBrush, new Rect(rect.X, rect.Bottom - thickness, rect.Width, thickness));
                break;

            case DropPosition.Inside:
                context.FillRectangle(InsideBrush, rect);
                context.DrawRectangle(new Pen(LineBrush, thickness), rect);
                break;
        }
    }

    private static ContentPresenter? GetHeaderPresenter(TreeViewItem container)
    {
        return container.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .FirstOrDefault(p => p.Name == "PART_HeaderPresenter");
    }
}
